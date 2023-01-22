using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
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
	protected IEventBusOptions EventBusOptions { get; }
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

		EventBusOptions = new EventBusOptions()
		{
			HostInfo = new HostInfo(configuration.EventBusName),
			HostLogger = configuration.HostLogger(serviceProvider),
			HandlerLogger = configuration.HandlerLogger(serviceProvider),
			MessageHandlerResultFactory = configuration.MessageHandlerResultFactory(serviceProvider),
			EventBodyProvider = configuration.EventBodyProvider
		};

		error = EventBusOptions.Validate(nameof(EventBusOptions));
		if (0 < error?.Count)
			throw new ConfigurationException(error);
	}

	/// <inheritdoc />
	public Task<IResult<Guid>> PublishAsync(
		IEvent @event,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(@event, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	/// <inheritdoc />
	public Task<IResult<Guid>> PublishAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(
			@event,
			optionsBuilder,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //EventBusOptions.HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	/// <inheritdoc />
	public Task<IResult<Guid>> PublishAsync(
		IEvent @event,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> PublishAsync(@event, null, traceInfo, cancellationToken);

	/// <inheritdoc />
	public async Task<IResult<Guid>> PublishAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (@event == null)
		{
			var result = new ResultBuilder<Guid>();
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionCoordinator = false;
		if (options.TransactionController == null)
		{
			options.TransactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		return await PublishInternalAsync(@event, options, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	protected async Task<IResult<Guid>> PublishInternalAsync(
		IEvent @event,
		IMessageOptions options,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//EventBusOptions.HostInfo.HostName
					),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		var transactionController = options.TransactionController;
		AsyncEventHandlerProcessor? handlerProcessor = null;
		MessageHandlerContext? handlerContext = null;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
			{
				var eventType = @event.GetType();

				var savedEventResult = await SaveEventAsync(@event, options, traceInfo, cancellationToken).ConfigureAwait(false);
				if (result.MergeHasError(savedEventResult))
					return result.Build();

				var savedEvent = savedEventResult.Data;

				if (savedEvent == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)} == null | {nameof(eventType)} = {eventType.FullName}");
				if (savedEvent.Message == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)}.{nameof(savedEvent.Message)} == null | {nameof(eventType)} = {eventType.FullName}");

				handlerContext = EventHandlerRegistry.CreateEventHandlerContext(eventType, ServiceProvider);

				var throwNoHandlerException = options.ThrowNoHandlerException ?? false;

				if (handlerContext == null)
				{
					if (throwNoHandlerException)
					{
						return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");
					}
					else
					{
						return result.WithWarning(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");
					}
				}

				handlerContext.MessageHandlerResultFactory = EventBusOptions.MessageHandlerResultFactory;
				handlerContext.TransactionController = transactionController;
				handlerContext.ServiceProvider = ServiceProvider;
				handlerContext.TraceInfo = traceInfo;
				handlerContext.HostInfo = EventBusOptions.HostInfo;
				handlerContext.HandlerLogger = EventBusOptions.HandlerLogger;
				handlerContext.MessageId = savedEvent.MessageId;
				handlerContext.DisabledMessagePersistence = options.DisabledMessagePersistence;
				handlerContext.ThrowNoHandlerException = throwNoHandlerException;
				handlerContext.PublisherId = PublisherHelper.GetPublisherIdentifier(EventBusOptions.HostInfo, traceInfo);
				handlerContext.PublishingTimeUtc = DateTime.UtcNow;
				handlerContext.ParentMessageId = null;
				handlerContext.Timeout = options.Timeout;
				handlerContext.RetryCount = 0;
				handlerContext.ErrorHandling = options.ErrorHandling;
				handlerContext.IdSession = options.IdSession;
				handlerContext.ContentType = options.ContentType;
				handlerContext.ContentEncoding = options.ContentEncoding;
				handlerContext.IsCompressedContent = options.IsCompressContent;
				handlerContext.IsEncryptedContent = options.IsEncryptContent;
				handlerContext.ContainsContent = true;
				handlerContext.Priority = options.Priority;
				handlerContext.Headers = options.Headers?.GetAll();

				handlerContext.Initialize(MessageStatus.Created, null);

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

				var handlerResult = await handlerProcessor.HandleAsync(savedEvent.Message, handlerContext, ServiceProvider, unhandledExceptionDetail, cancellationToken).ConfigureAwait(false);
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

				return result.WithData(savedEvent.MessageId).Build();
			},
			$"{nameof(PublishAsync)}<{@event?.GetType().FullName}>",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await EventBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						EventBusOptions.HostInfo,
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

	protected virtual async Task<IResult<ISavedMessage<TEvent>>> SaveEventAsync<TEvent>(
		TEvent @event,
		IMessageOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TEvent : class, IEvent
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TEvent>>();

		var utcNow = DateTime.UtcNow;
		var metadata = new MessageMetadata<TEvent>
		{
			MessageId = Guid.NewGuid(),
			Message = @event,
			ParentMessageId = null,
			PublishingTimeUtc = utcNow,
			PublisherId = "--EventBus--",
			TraceInfo = traceInfo,
			Timeout = options.Timeout,
			RetryCount = 0,
			ErrorHandling = options.ErrorHandling,
			IdSession = options.IdSession,
			ContentType = options.ContentType,
			ContentEncoding = options.ContentEncoding,
			IsCompressedContent = options.IsCompressContent,
			IsEncryptedContent = options.IsEncryptContent,
			ContainsContent = @event != null,
			Priority = options.Priority,
			Headers = options.Headers?.GetAll(),
			DisabledMessagePersistence = options.DisabledMessagePersistence,
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null
		};

		if (EventBusOptions.EventBodyProvider != null
			&& EventBusOptions.EventBodyProvider.AllowMessagePersistence(options.DisabledMessagePersistence, metadata))
		{
			var saveResult = await EventBusOptions.EventBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, @event, traceInfo, options.TransactionController, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	#region IEventPublisher

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo.Create(ServiceProvider.GetRequiredService<IApplicationContext>(), null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		var isLocalTransactionCoordinator = false;
		if (options.TransactionController == null)
		{
			options.TransactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var publishResult = await PublishInternalAsync(@event, options, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo.Create(ServiceProvider.GetRequiredService<IApplicationContext>(), null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionCoordinator = false;
		if (options.TransactionController == null)
		{
			options.TransactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var publishResult = await PublishInternalAsync(@event, options, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		var isLocalTransactionCoordinator = false;
		if (options.TransactionController == null)
		{
			options.TransactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var publishResult = await PublishInternalAsync(@event, options, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionCoordinator = false;
		if (options.TransactionController == null)
		{
			options.TransactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var publishResult = await PublishInternalAsync(@event, options, isLocalTransactionCoordinator, traceInfo, cancellationToken).ConfigureAwait(false);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	#endregion IEventPublisher
}
