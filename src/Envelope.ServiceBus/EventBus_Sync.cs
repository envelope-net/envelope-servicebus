using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public partial class EventBus : IEventBus
{
	/// <inheritdoc />
	public IResult<Guid> Publish(
		IEvent @event,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Publish(@event, null!, memberName, sourceFilePath, sourceLineNumber);

	/// <inheritdoc />
	public IResult<Guid> Publish(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Publish(
			@event,
			optionsBuilder,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //EventBusOptions.HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber));

	/// <inheritdoc />
	public IResult<Guid> Publish(
		IEvent @event,
		ITraceInfo traceInfo)
		=> Publish(@event, null, traceInfo);

	/// <inheritdoc />
	public IResult<Guid> Publish(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo)
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

		return Publish(@event, options, isLocalTransactionCoordinator, traceInfo);
	}

	protected IResult<Guid> Publish(
		IEvent @event,
		IMessageOptions options,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo)
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

		return ServiceTransactionInterceptor.ExecuteAction(
			false,
			traceInfo,
			transactionController,
			(traceInfo, transactionController) =>
			{
				var eventType = @event.GetType();

				var savedEventResult = SaveEvent(@event, options, traceInfo);
				if (result.MergeHasError(savedEventResult))
					return result.Build();

				var savedEvent = savedEventResult.Data;

				if (savedEvent == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)} == null | {nameof(eventType)} = {eventType.FullName}");
				if (savedEvent.Message == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)}.{nameof(savedEvent.Message)} == null | {nameof(eventType)} = {eventType.FullName}");

				var handlerContext = EventHandlerRegistry.CreateEventHandlerContext(eventType, ServiceProvider);

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

				var handlerProcessor = (EventHandlerProcessor)_asyncVoidEventHandlerProcessors.GetOrAdd(
					eventType,
					eventType =>
					{
						var processor = Activator.CreateInstance(typeof(EventHandlerProcessor<,>).MakeGenericType(eventType, handlerContext.GetType())) as EventHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

				var handlerResult = handlerProcessor.Handle(savedEvent.Message, handlerContext, ServiceProvider);
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
			$"{nameof(Publish)}<{@event?.GetType().FullName}>",
			(traceInfo, exception, detail) =>
			{
				var errorMessage =
					EventBusOptions.HostLogger.LogError(
						traceInfo,
						EventBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null);

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator);
	}

	protected virtual IResult<ISavedMessage<TEvent>> SaveEvent<TEvent>(
		TEvent @event,
		IMessageOptions options,
		ITraceInfo traceInfo)
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
			var saveResult = EventBusOptions.EventBodyProvider.SaveToStorage(new List<IMessageMetadata> { metadata }, @event, traceInfo, options.TransactionController);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}
}
