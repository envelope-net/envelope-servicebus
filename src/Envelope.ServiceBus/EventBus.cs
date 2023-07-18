using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public partial class EventBus : IEventBus
{
	protected IServiceProvider ServiceProvider { get; }
	public IHostInfo HostInfo { get; }
	public IHostLogger HostLogger { get; }
	public IHandlerLogger HandlerLogger { get; }
	protected IEventHandlerRegistry EventHandlerRegistry { get; }

	private static readonly ConcurrentDictionary<Type, EventHandlerProcessorBase> _asyncEventHandlerProcessors = new();
	private static readonly ConcurrentDictionary<Type, EventHandlerProcessorBase> _asyncVoidEventHandlerProcessors = new();

	public EventBus(IServiceProvider serviceProvider, IEventBusConfiguration configuration, IEventHandlerRegistry eventHandlerRegistry)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		EventHandlerRegistry = eventHandlerRegistry ?? throw new ArgumentNullException(nameof(eventHandlerRegistry));

		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		var error = configuration.Validate(nameof(EventBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		HostInfo = new HostInfo(configuration.EventBusName) ?? throw new InvalidOperationException($"{nameof(HostInfo)} == null");
		HostLogger = configuration.HostLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(HostLogger)} == null");
		HandlerLogger = configuration.HandlerLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(HandlerLogger)} == null");
	}

	/// <inheritdoc />
	public Task<IResult> PublishAsync(
		IEvent @event,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(@event, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	/// <inheritdoc />
	public Task<IResult> PublishAsync(
		IEvent @event,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(
			@event,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	/// <inheritdoc />
	public Task<IResult> PublishAsync(
		IEvent @event,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> PublishAsync(@event, null!, traceInfo, cancellationToken);

	/// <inheritdoc />
	public async Task<IResult> PublishAsync(
		IEvent @event,
		ITransactionController transactionController,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (@event == null)
		{
			var result = new ResultBuilder();
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		}

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		return await PublishInternalAsync(@event, transactionController, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	protected async Task<IResult> PublishInternalAsync(
		IEvent @event,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//HostInfo.HostName
					),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		AsyncEventHandlerProcessor? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
			{
				var eventType = @event.GetType();

				handlerContext = EventHandlerRegistry.CreateEventHandlerContext(eventType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (AsyncEventHandlerProcessor)_asyncVoidEventHandlerProcessors.GetOrAdd(
					eventType,
					eventType =>
					{
						var processor = Activator.CreateInstance(typeof(AsyncEventHandlerProcessor<,>).MakeGenericType(eventType, handlerContext.GetType())) as EventHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

				var handlerResult = await handlerProcessor.HandleAsync(@event, handlerContext, ServiceProvider, unhandledExceptionDetail, cancellationToken).ConfigureAwait(false);
				result.MergeAll(handlerResult);

				if (result.HasTransactionRollbackError())
				{
					transactionController.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionCoordinator)
						transactionController.ScheduleCommit();
				}

				return result.Build();
			},
			$"{nameof(PublishAsync)}<{@event?.GetType().FullName}>",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await HostLogger.LogErrorAsync(
						traceInfo,
						HostInfo,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				if (handlerProcessor != null)
				{
					try
					{
						await handlerProcessor.OnErrorAsync(traceInfo, exception, null, detail, @event, handlerContext, ServiceProvider, cancellationToken);
					}
					catch { }
				}

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator,
			cancellationToken).ConfigureAwait(false);
	}

	protected virtual ITransactionController CreateTransactionController()
		=> ServiceProvider.GetRequiredService<ITransactionCoordinator>().TransactionController;
}
