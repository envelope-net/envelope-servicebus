using Envelope.Converters;
using Envelope.Extensions;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Model;
using Envelope.ServiceBus.Model.Internal;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Services;
using Envelope.Threading;
using Envelope.Trace;

namespace Envelope.ServiceBus.Queues.Internal;

internal class InMemoryMessageQueue<TMessage> : IMessageQueue<TMessage>, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	private readonly IQueue _queue;
	private readonly IMessageQueueConfiguration<TMessage> _configuration;
	private bool disposed;

	/// <inheritdoc/>
	public Guid QueueId { get; }

	/// <inheritdoc/>
	public string QueueName { get; }

	/// <inheritdoc/>
	public bool IsPersistent => false;

	/// <inheritdoc/>
	public bool IsFaultQueue => false;

	/// <inheritdoc/>
	public QueueType QueueType { get; }

	/// <inheritdoc/>
	public bool IsPull { get; }

	/// <inheritdoc/>
	public long Count => _queue.Count;

	/// <inheritdoc/>
	public int? MaxSize { get; }

	/// <inheritdoc/>
	public TimeSpan? DefaultProcessingTimeout { get; }

	/// <inheritdoc/>
	public TimeSpan FetchInterval { get; set; }

	public HandleMessage<TMessage>? MessageHandler { get; }

	public InMemoryMessageQueue(IMessageQueueConfiguration<TMessage> configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		QueueName = _configuration.QueueName;
		QueueId = GuidConverter.ToGuid(QueueName);
		IsPull = _configuration.IsPull;
		QueueType = _configuration.QueueType;
		MaxSize = _configuration.MaxSize;

		if (QueueType == QueueType.Sequential_FIFO)
		{
			_queue = new InMemoryFIFOQueue(MaxSize);
		}
		else if (QueueType == QueueType.Sequential_Delayable)
		{
			_queue = new InMemoryDelayableQueue(MaxSize);
		}
		else
		{
			throw new NotImplementedException(QueueType.ToString());
		}

		DefaultProcessingTimeout = _configuration.DefaultProcessingTimeout;
		MessageHandler = _configuration.MessageHandler;
	}

	/// <inheritdoc/>
	public async Task<IResult> EnqueueAsync(TMessage? message, IQueueEnqueueContext context, CancellationToken cancellationToken)
	{
		var traceInfo = TraceInfo.Create(context.TraceInfo);
		var result = new ResultBuilder<Guid>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName}", new ObjectDisposedException(GetType().FullName));

		var inMemoryQueuedMessage = QueuedMessageFactory<TMessage>.CreateQueuedMessage(message, context, _configuration);

		var messagesMetadata = new List<IMessageMetadata> { inMemoryQueuedMessage };
		if (!context.DisabledMessagePersistence)
		{
			try
			{
				var saveResult =
					await _configuration.MessageBodyProvider.SaveToStorageAsync(
						messagesMetadata,
						message,
						traceInfo,
						cancellationToken);

				if (result.MergeHasError(saveResult))
					return await PublishQueueEventAsync(
						traceInfo,
						QueueEventType.Enqueue,
						result.Build());
			}
			catch (Exception ex)
			{
				return await PublishQueueEventAsync(
						traceInfo,
						QueueEventType.Enqueue,
						result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName}", ex));
			}
		}

		if (IsPull)
		{
			var enqueueResult = await _queue.EnqueueAsync(messagesMetadata, traceInfo);
			if (result.MergeHasError(enqueueResult))
				return await PublishQueueEventAsync(traceInfo, QueueEventType.Enqueue, result.Build());
		}
		else //IsPush
		{
			if (MessageHandler == null)
				return await PublishQueueEventAsync(
						traceInfo,
						QueueEventType.Enqueue,
						result.WithInvalidOperationException(traceInfo, $"{nameof(IsPull)} = {IsPull} | {nameof(MessageHandler)} == null"));

			if (context.IsAsynchronousInvocation)
			{
				var enqueueResult = await _queue.EnqueueAsync(messagesMetadata, traceInfo);
				if (result.MergeHasError(enqueueResult))
					return await PublishQueueEventAsync(traceInfo, QueueEventType.Enqueue, result.Build());

				_ = Task.Run(async () => await OnMessageAsync(traceInfo, cancellationToken), cancellationToken); //do not wait
			}
			else //is synchronous invocation
			{
				var handlerResult = await HandleMessageAsync(inMemoryQueuedMessage, traceInfo, cancellationToken);
				result.MergeHasError(handlerResult);
				if (handlerResult.Data?.Processed == false)
					result.WithError(traceInfo, x => x.InternalMessage(handlerResult.Data.ToString()));
			}
		}

		return await PublishQueueEventAsync(traceInfo, QueueEventType.Enqueue, result.Build());
	}

	/// <inheritdoc/>
	public async Task<IResult> TryRemoveAsync(IQueuedMessage<TMessage> message, ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<Guid>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName}", new ObjectDisposedException(GetType().FullName));

		if (IsPull)
			return await PublishQueueEventAsync(
				traceInfo,
				QueueEventType.Remove,
				(IResult)result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName} | { nameof(TryRemoveAsync)}: {nameof(IsPull)} = {IsPull}"));

		var removeResult = await _queue.TryRemoveAsync(message, traceInfo);
		result.MergeHasError(removeResult);
		return await PublishQueueEventAsync(
			traceInfo,
			QueueEventType.Remove,
			(IResult)result.Build());
	}

	/// <inheritdoc/>
	public async Task<IResult<IQueuedMessage<TMessage>?>> TryPeekAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IQueuedMessage<TMessage>?>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName}", new ObjectDisposedException(GetType().FullName));

		var peekResult = await _queue.TryPeekAsync(traceInfo);
		if (result.MergeHasError(peekResult))
			return await PublishQueueEventAsync(traceInfo, QueueEventType.Peek, result.Build());

		var messageHeader = peekResult.Data;

		if (messageHeader == null)
			return await PublishQueueEventAsync(
				traceInfo,
				QueueEventType.Peek,
				result.Build());

		if (messageHeader is not IQueuedMessage<TMessage> inMemoryQueuedMessage)
			return await PublishQueueEventAsync(
					traceInfo,
					QueueEventType.Peek,
					result.WithInvalidOperationException(
					traceInfo,
					$"QueueName = {_configuration.QueueName} | { nameof(_queue)} must by type of {typeof(IQueuedMessage<TMessage>).FullName} but {messageHeader.GetType().FullName} found."));

		if (!inMemoryQueuedMessage.DisabledMessagePersistence)
		{
			try
			{
				var loadResult = await _configuration.MessageBodyProvider.LoadFromStorageAsync<TMessage>(inMemoryQueuedMessage, traceInfo, cancellationToken);
				if (result.MergeHasError(loadResult))
					return await PublishQueueEventAsync(traceInfo, QueueEventType.Peek, result.Build());

				var message = loadResult.Data;

				//kedze plati ContainsContent = true
				if (message == null)
					return await PublishQueueEventAsync(
						traceInfo,
						QueueEventType.Peek,
						result.WithInvalidOperationException(
							traceInfo,
							$"QueueName = {_configuration.QueueName} | {nameof(TryPeekAsync)}: {nameof(inMemoryQueuedMessage.QueueName)} == {inMemoryQueuedMessage.QueueName} | {nameof(inMemoryQueuedMessage.MessageId)} == {inMemoryQueuedMessage.MessageId} | {nameof(message)} == null"));

				inMemoryQueuedMessage.Message = message;
			}
			catch (Exception ex)
			{
				return await PublishQueueEventAsync(
					traceInfo,
					QueueEventType.Peek,
					result.WithInvalidOperationException(traceInfo, $"QueueName = {_configuration.QueueName}", ex));
			}
		}

		return await PublishQueueEventAsync(
			traceInfo,
			QueueEventType.Peek,
			result.WithData(inMemoryQueuedMessage).Build());
	}

	private readonly AsyncLock _onMessageLock = new();
	private async Task OnMessageAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		if (disposed)
			return;

		traceInfo = TraceInfo.Create(traceInfo);

		using (await _onMessageLock.LockAsync().ConfigureAwait(false))
		{
			if (disposed)
				return;

			while (0 < _queue.Count)
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				var peekResult = await TryPeekAsync(traceInfo, cancellationToken);
				if (peekResult.HasError)
				{
					await _configuration.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(peekResult, null, cancellationToken);
					return;
				}

				var message = peekResult.Data;

				if (message != null)
				{
					if (message.Processed)
					{
						var removeResult = await _queue.TryRemoveAsync(message, traceInfo);
						if (removeResult.HasError)
						{
							await _configuration.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken);
							await PublishQueueEventAsync(traceInfo, QueueEventType.OnMessage, removeResult);
						}

						continue;
					}

					var nowUtc = DateTime.UtcNow;
					if (message.TimeToLiveUtc < nowUtc)
					{
						//TODO: timeout - zahod msg - ze by do FaultQueue?
					}

					var handlerResult = await HandleMessageAsync(message, traceInfo, cancellationToken);
					if (handlerResult.Data!.Processed)
					{
						var removeResult = await _queue.TryRemoveAsync(message, traceInfo);
						if (removeResult.HasError)
						{
							await _configuration.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken);
							await PublishQueueEventAsync(traceInfo, QueueEventType.OnMessage, removeResult);
						}
					}
				}
			}
		}
	}

	private async Task<IResult<IMessageMetadataUpdate>> HandleMessageAsync(IQueuedMessage<TMessage> message, ITraceInfo traceInfo,CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IMessageMetadataUpdate>();

		var processingTimeout = DefaultProcessingTimeout;

		var handlerContext = _configuration.ServiceBusOptions.MessageHandlerContextFactory(_configuration.ServiceBusOptions.ServiceProvider);
		handlerContext.MessageHandlerResultFactory = _configuration.ServiceBusOptions.MessageHandlerResultFactory;
		handlerContext.TransactionContext = null;                                                                  //TODO:  transactionContext;
		handlerContext.ServiceProvider = _configuration.ServiceBusOptions.ServiceProvider;
		handlerContext.TraceInfo = TraceInfo.Create(traceInfo);
		handlerContext.HostInfo = _configuration.ServiceBusOptions.HostInfo;
		handlerContext.HandlerLogger = _configuration.ServiceBusOptions.HandlerLogger;
		handlerContext.MessageId = message.MessageId;
		handlerContext.DisabledMessagePersistence = message.DisabledMessagePersistence;
		handlerContext.ThrowNoHandlerException = true;
		handlerContext.PublisherId = PublisherHelper.GetPublisherIdentifier(_configuration.ServiceBusOptions.HostInfo, traceInfo);
		handlerContext.PublishingTimeUtc = message.PublishingTimeUtc;
		handlerContext.ParentMessageId = message.ParentMessageId;
		handlerContext.Timeout = message.Timeout;
		handlerContext.RetryCount = message.RetryCount;
		handlerContext.ErrorHandling = message.ErrorHandling;
		handlerContext.IdSession = message.IdSession;
		handlerContext.ContentType = message.ContentType;
		handlerContext.ContentEncoding = message.ContentEncoding;
		handlerContext.IsCompressedContent = message.IsCompressedContent;
		handlerContext.IsEncryptedContent = message.IsEncryptedContent;
		handlerContext.ContainsContent = message.ContainsContent;
		handlerContext.Priority = message.Priority;
		handlerContext.Headers = message.Headers;

		handlerContext.Initialize(message.MessageStatus, message.DelayedToUtc);

		var task = MessageHandler!(message, handlerContext, cancellationToken);
		if (processingTimeout.HasValue)
			task = task.OrTimeoutAsync(processingTimeout.Value);

		var handlerResult = await task.ConfigureAwait(false);
		if (handlerResult == null)
		{
			result.WithInvalidOperationException(traceInfo, $"{nameof(handlerResult)} == null");

			await _configuration.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken);
			return await PublishQueueEventAsync(traceInfo, QueueEventType.OnMessage, result.Build());
		}

		var hasError = handlerResult.ErrorResult?.HasError == true;
		if (hasError)
		{
			result.MergeAllHasError(handlerResult.ErrorResult!);
			await _configuration.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken);
			await PublishQueueEventAsync(traceInfo, QueueEventType.OnMessage, result.Build());
		}

		var update = new MessageMetadataUpdate(message.MessageId)
		{
			MessageStatus = handlerResult.MessageStatus
		};

		if (update.MessageStatus != MessageStatus.Completed)
		{
			if (handlerResult.Retry)
			{
				var errorController = message.ErrorHandling ?? _configuration.ErrorHandling;
				var canRetry = errorController?.CanRetry(message.RetryCount);

				var retryed = false;
				if (canRetry == true)
				{
					var retryInterval = handlerResult.RetryInterval ?? errorController!.GetRetryTimeSpan(message.RetryCount);
					if (retryInterval.HasValue)
					{
						retryed = true;
						update.RetryCount = message.RetryCount + 1;
						update.DelayedToUtc = handlerResult.GetDelayedToUtc(retryInterval.Value);
					}
				}

				if (!retryed)
					update.MessageStatus = MessageStatus.Suspended;
			}
			else if (update.MessageStatus == MessageStatus.Deferred && handlerResult.RetryInterval.HasValue)
			{
				update.DelayedToUtc = handlerResult.GetDelayedToUtc(handlerResult.RetryInterval.Value);
			}
		}

		update.Processed = update.MessageStatus == MessageStatus.Completed;
		//TODO save message to DB (for postgresSQL queue within transaction?)

		return await PublishQueueEventAsync(
			traceInfo,
			QueueEventType.OnMessage,
			result.WithData(update).Build());
	}

	private async Task<TResult> PublishQueueEventAsync<TResult>(ITraceInfo traceInfo, QueueEventType queueEventType, TResult? result)
		where TResult : IResult
	{
		IQueueEvent queueEvent;
		if (result?.HasError == true)
			queueEvent = new QueueErrorEvent(this, queueEventType, result);
		else
			queueEvent = new QueueEvent(this, queueEventType);

		await _configuration.ServiceBusOptions.ServiceBusLifeCycleEventManager.PublishServiceBusEventInternalAsync(
			queueEvent,
			traceInfo,
			_configuration.ServiceBusOptions);

		return result!;
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCoreAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCoreAsync()
	{
		_queue.Dispose();
		return ValueTask.CompletedTask;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				_queue.Dispose();
			}

			disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
