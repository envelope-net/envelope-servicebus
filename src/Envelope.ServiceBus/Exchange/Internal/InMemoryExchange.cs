using Envelope.Converters;
using Envelope.Extensions;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Model;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Threading;
using Envelope.Trace;

namespace Envelope.ServiceBus.Exchange.Internal;

internal partial class InMemoryExchange<TMessage> : IExchange<TMessage>, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	private readonly IQueue _queue;
	private readonly ExchangeContext<TMessage> _exchangeContext;
	private bool disposed;

	public Guid ExchangeId { get; }

	public string ExchangeName { get; }

	Guid IQueueInfo.QueueId => ExchangeId;

	string IQueueInfo.QueueName => ExchangeName;

	public ExchangeType ExchangeType => _exchangeContext.Router.ExchangeType;

	/// <inheritdoc/>
	public bool IsPersistent => false;

	/// <inheritdoc/>
	public bool IsFaultQueue => false;

	/// <inheritdoc/>
	public QueueType QueueType { get; }

	/// <inheritdoc/>
	public long Count => _queue.Count;

	/// <inheritdoc/>
	public int? MaxSize { get; }

	public InMemoryExchange(ExchangeContext<TMessage> exchangeContext)
	{
		_exchangeContext = exchangeContext ?? throw new ArgumentNullException(nameof(exchangeContext));
		ExchangeName = _exchangeContext.ExchangeName;
		ExchangeId = GuidConverter.ToGuid(ExchangeName);
		QueueType = _exchangeContext.QueueType;
		MaxSize = _exchangeContext.MaxSize;

		if (QueueType == QueueType.Sequential_FIFO)
		{
			_queue = _exchangeContext.FIFOQueue;
		}
		else if (QueueType == QueueType.Sequential_Delayable)
		{
			_queue = _exchangeContext.DelayableQueue;
		}
		else
		{
			throw new NotImplementedException(QueueType.ToString());
		}

		_queue.MaxSize = MaxSize;
	}

	/// <inheritdoc/>
	public async Task<IResult<List<Guid>>> EnqueueAsync(TMessage? message, IExchangeEnqueueContext context, CancellationToken cancellationToken)
	{
		var traceInfo = TraceInfo.Create(context.TraceInfo);
		var result = new ResultBuilder<List<Guid>>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		var createResult = _exchangeContext.ExchangeMessageFactory.CreateExchangeMessages(message, context, _exchangeContext, traceInfo);
		if (result.MergeHasError(createResult))
			return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Enqueue, result.Build());

		var exchangeMessages = createResult.Data;
		if (exchangeMessages == null || exchangeMessages.Count == 0)
			return await PublishExchangeEventAsync(
				traceInfo,
				ExchangeEventType.Enqueue,
				result.WithInvalidOperationException(traceInfo, $"No exchange messages was created for {typeof(TMessage).FullName} | ExchangeName = {_exchangeContext.ExchangeName}"));

		if (!context.IsAsynchronousInvocation && 1 < exchangeMessages.Count)
			return await PublishExchangeEventAsync(
				traceInfo,
				ExchangeEventType.Enqueue,
				result.WithInvalidOperationException(traceInfo, $"Multiple messages for synchronous invocation is not allowed. MessageType = {typeof(TMessage).FullName} | ExchangeName = {_exchangeContext.ExchangeName}"));

		if (!context.DisabledMessagePersistence)
		{
			try
			{
				var saveResult = 
					await _exchangeContext.MessageBodyProvider.SaveToStorageAsync(
						exchangeMessages.Cast<IMessageMetadata>().ToList(),
						message,
						traceInfo,
						cancellationToken);

				if (result.MergeHasError(saveResult))
					return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Enqueue, result.Build());
			}
			catch (Exception ex)
			{
				return await PublishExchangeEventAsync(
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", ex));
			}
		}

		if (context.IsAsynchronousInvocation)
		{
			var enqueueResult = await _queue.EnqueueAsync(exchangeMessages.Cast<IMessageMetadata>().ToList(), traceInfo);
			if (result.MergeHasError(enqueueResult))
				return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Enqueue, result.Build());

			_ = Task.Run(async () => await OnMessageAsync(traceInfo, cancellationToken), cancellationToken); //do not wait
			return await PublishExchangeEventAsync(
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithData(exchangeMessages.Select(x => x.MessageId).ToList()).Build());
		}
		else
		{
			var exchangeMessage = exchangeMessages[0];
			var brokerResult = await _exchangeContext.MessageBrokerHandler.HandleAsync(exchangeMessage, _exchangeContext, cancellationToken).ConfigureAwait(false);

			if (brokerResult.ErrorResult?.HasError == true)
				result.MergeHasError(brokerResult.ErrorResult!);

			var handlerResult = await ProcessMessageHandlerResultAsync(exchangeMessage, traceInfo, brokerResult, cancellationToken);
			result.MergeHasError(handlerResult);
			if (handlerResult.Data?.Processed == false)
				result.WithError(traceInfo, x => x.InternalMessage(handlerResult.Data.ToString()));

			return await PublishExchangeEventAsync(
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithData(new List<Guid> { exchangeMessage.MessageId }).Build());
		}
	}

	/// <inheritdoc/>
	public async Task<IResult> TryRemoveAsync(IExchangeMessage<TMessage> message, ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<Guid>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		var removeResult = await _queue.TryRemoveAsync(message, traceInfo);
		result.MergeHasError(removeResult);
		return 
			await PublishExchangeEventAsync(
				traceInfo,
				ExchangeEventType.Remove,
				(IResult)result.Build());
	}

	/// <inheritdoc/>
	public async Task<IResult<IExchangeMessage<TMessage>?>> TryPeekAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IExchangeMessage<TMessage>?>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		var peekResult = await _queue.TryPeekAsync(traceInfo);
		if (result.MergeHasError(peekResult))
			return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Peek, result.Build());

		var messageHeader = peekResult.Data;

		if (messageHeader == null)
			return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Peek, result.Build());

		if (messageHeader is not IExchangeMessage<TMessage> inMemoryExchangeMessage)
			return await PublishExchangeEventAsync(
					traceInfo,
					ExchangeEventType.Peek,
					result.WithInvalidOperationException(
						traceInfo,
						$"ExchangeName = {_exchangeContext.ExchangeName} | {nameof(_queue)} must by type of {typeof(IExchangeMessage<TMessage>).FullName} but {messageHeader.GetType().FullName} found."));

		if (!inMemoryExchangeMessage.DisabledMessagePersistence)
		{
			try
			{
				var loadResult = await _exchangeContext.MessageBodyProvider.LoadFromStorageAsync<TMessage>(inMemoryExchangeMessage, traceInfo, cancellationToken);
				if (result.MergeHasError(loadResult))
					return await PublishExchangeEventAsync(traceInfo, ExchangeEventType.Peek, result.Build());

				var message = loadResult.Data;

				//kedze plati ContainsContent = true
				if (message == null)
					return await PublishExchangeEventAsync(
						traceInfo,
						ExchangeEventType.Peek,
						result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName} | {nameof(TryPeekAsync)}: {nameof(inMemoryExchangeMessage.ExchangeName)} == {inMemoryExchangeMessage.ExchangeName} | {nameof(inMemoryExchangeMessage.MessageId)} == {inMemoryExchangeMessage.MessageId} | {nameof(message)} == null"));

				inMemoryExchangeMessage.Message = message;
			}
			catch (Exception ex)
			{
				return await PublishExchangeEventAsync(
					traceInfo,
					ExchangeEventType.Peek,
					result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", ex));
			}
		}

		return await PublishExchangeEventAsync(
			traceInfo,
			ExchangeEventType.Peek,
			result.WithData(inMemoryExchangeMessage).Build());
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
					await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(peekResult, null, cancellationToken);
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
							await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken);
							await PublishExchangeEventAsync(traceInfo, ExchangeEventType.OnMessage, removeResult);
						}

						continue;
					}

					var nowUtc = DateTime.UtcNow;

					if (message.TimeToLiveUtc < nowUtc)
					{
						//TODO: timeout - zahod msg - ze by do FaultQueue?
					}

					var brokerResult = await _exchangeContext.MessageBrokerHandler.HandleAsync(message, _exchangeContext, cancellationToken).ConfigureAwait(false);

					var handlerResult = await ProcessMessageHandlerResultAsync(message, traceInfo, brokerResult, cancellationToken);
					if (handlerResult.Data!.Processed)
					{
						var removeResult = await _queue.TryRemoveAsync(message, traceInfo);
						if (removeResult.HasError)
						{
							await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken);
							await PublishExchangeEventAsync(traceInfo, ExchangeEventType.OnMessage, removeResult);
						}
					}
				}
			}
		}
	}

	private async Task<IResult<IMessageMetadataUpdate>> ProcessMessageHandlerResultAsync(
		IExchangeMessage<TMessage> message,
		ITraceInfo traceInfo,
		MessageHandlerResult brokerResult,
		CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IMessageMetadataUpdate>();

		var hasError = brokerResult.ErrorResult?.HasError == true;

		if (hasError)
		{
			result.MergeAllHasError(brokerResult.ErrorResult!);
			await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken);
			await PublishExchangeEventAsync(traceInfo, ExchangeEventType.OnMessage, result.Build());
		}

		var update = new MessageMetadataUpdate(message.MessageId)
		{
			MessageStatus = brokerResult.MessageStatus
		};

		if (update.MessageStatus != MessageStatus.Delivered)
		{
			if (brokerResult.Retry)
			{
				var errorController = message.ErrorHandling ?? _exchangeContext.ErrorHandling;
				var canRetry = errorController?.CanRetry(message.RetryCount);

				var retryed = false;
				if (canRetry == true)
				{
					var retryInterval = brokerResult.RetryInterval ?? errorController!.GetRetryTimeSpan(message.RetryCount);
					if (retryInterval.HasValue)
					{
						retryed = true;
						update.RetryCount = message.RetryCount + 1;
						update.DelayedToUtc = brokerResult.GetDelayedToUtc(retryInterval.Value);
					}
				}

				if (!retryed)
					update.MessageStatus = MessageStatus.Suspended;
			}
			else if (update.MessageStatus == MessageStatus.Deferred && brokerResult.RetryInterval.HasValue)
			{
				update.DelayedToUtc = brokerResult.GetDelayedToUtc(brokerResult.RetryInterval.Value);
			}
		}

		update.Processed = update.MessageStatus == MessageStatus.Delivered;
		//TODO save message to DB (for postgresSQL exchange within transaction?)

		return await PublishExchangeEventAsync(
			traceInfo,
			ExchangeEventType.OnMessage,
			result.WithData(update).Build());
	}

	private async Task<TResult> PublishExchangeEventAsync<TResult>(ITraceInfo traceInfo, ExchangeEventType exchangeEventType, TResult result)
		where TResult : IResult
	{
		IExchangeEvent exchangeEvent;
		if (result.HasError)
			exchangeEvent = new ExchangeErrorEvent(this, exchangeEventType, result);
		else
			exchangeEvent = new ExchangeEvent(this, exchangeEventType);

		await _exchangeContext.ServiceBusOptions.ServiceBusLifeCycleEventManager.PublishServiceBusEventInternalAsync(
			exchangeEvent,
			traceInfo,
			_exchangeContext.ServiceBusOptions);

		return result;
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
