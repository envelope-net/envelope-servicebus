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

public partial class MessageBus : IMessageBus
{
	protected IServiceProvider ServiceProvider { get; }
	public IHostInfo HostInfo { get; }
	public IHostLogger HostLogger { get; }
	public IHandlerLogger HandlerLogger { get; }
	protected IMessageHandlerRegistry MessageHandlerRegistry { get; }

	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncMessageHandlerProcessors = new();
	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncVoidMessageHandlerProcessors = new();

	public MessageBus(IServiceProvider serviceProvider, IMessageBusConfiguration configuration, IMessageHandlerRegistry messageHandlerRegistry)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		MessageHandlerRegistry = messageHandlerRegistry ?? throw new ArgumentNullException(nameof(messageHandlerRegistry));

		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		var error = configuration.Validate(nameof(MessageBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		HostInfo = new HostInfo(configuration.MessageBusName) ?? throw new InvalidOperationException($"{nameof(HostInfo)} == null");
		HostLogger = configuration.HostLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(HostLogger)} == null");
		HandlerLogger = configuration.HandlerLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(HandlerLogger)} == null");
	}

	public Task<IResult> SendAsync(
		IRequestMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult> SendAsync(
		IRequestMessage message,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(
			message,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	public Task<IResult> SendAsync(
		IRequestMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, null!, traceInfo, cancellationToken);

	public async Task<IResult> SendAsync(
		IRequestMessage message,
		ITransactionController transactionController,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var sendResult = await SendInternalAsync(message, transactionController, isLocalTransactionCoordinator, traceInfo, cancellationToken);
		return sendResult;
	}

	protected async Task<IResult> SendInternalAsync(
		IRequestMessage message,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
					HostInfo.HostName),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		AsyncVoidMessageHandlerProcessor? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
			{
				var requestMessageType = message.GetType();

				handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (AsyncVoidMessageHandlerProcessor)_asyncVoidMessageHandlerProcessors.GetOrAdd(
					requestMessageType,
					requestMessageType =>
					{
						var processor = Activator.CreateInstance(typeof(AsyncVoidMessageHandlerProcessor<,>).MakeGenericType(requestMessageType, handlerContext.GetType())) as MessageHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

				var handlerResult = await handlerProcessor.HandleAsync(message, handlerContext, ServiceProvider, unhandledExceptionDetail, cancellationToken).ConfigureAwait(false);
				result.MergeAllHasError(handlerResult);

				if (result.HasError())
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
			$"{nameof(SendAsync)}<{message?.GetType().FullName}> return {typeof(IResult<Guid>).FullName}",
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
						await handlerProcessor.OnErrorAsync(traceInfo, exception, null, detail, message, handlerContext, ServiceProvider, cancellationToken);
					}
					catch { }
				}

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator,
			cancellationToken).ConfigureAwait(false);
	}

	public Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(
			message,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	public Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, null!, traceInfo, cancellationToken);

	public async Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<TResponse>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var sendResult = await SendInternalAsync(message, transactionController, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
		result.MergeAllHasError(sendResult);

		if (sendResult.Data != null)
			result.WithData(sendResult.Data);

		return result.Build();
	}

	protected async Task<IResult<TResponse>> SendInternalAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<TResponse>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
					HostInfo.HostName),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		AsyncMessageHandlerProcessor<TResponse>? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
			{
				var requestMessageType = message.GetType();

				handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (AsyncMessageHandlerProcessor<TResponse>)_asyncVoidMessageHandlerProcessors.GetOrAdd(
					requestMessageType,
					requestMessageType =>
					{
						var processor = Activator.CreateInstance(typeof(AsyncMessageHandlerProcessor<,,>).MakeGenericType(requestMessageType, typeof(TResponse), handlerContext.GetType())) as MessageHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

				var handlerResult = await handlerProcessor.HandleAsync(message, handlerContext, ServiceProvider, traceInfo, unhandledExceptionDetail, cancellationToken).ConfigureAwait(false);
				result.MergeAllWithData(handlerResult);

				if (result.HasError())
				{
					transactionController.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionCoordinator)
						transactionController.ScheduleCommit();
				}

				return result.WithData(handlerResult.Data).Build();
			},
			$"{nameof(SendAsync)}<{message?.GetType().FullName}> return {typeof(IResult<TResponse>).FullName}",
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
						await handlerProcessor.OnErrorAsync(traceInfo, exception, null, detail, message, handlerContext, ServiceProvider, cancellationToken);
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
