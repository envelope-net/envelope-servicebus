using Envelope.Converters;
using Envelope.Extensions;
using Envelope.Infrastructure;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Model;
using Envelope.ServiceBus.Model.Internal;
using Envelope.ServiceBus.Queues.Internal;
using Envelope.Services;
using Envelope.Threading;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues;

public class MessageQueue<TMessage> : IMessageQueue<TMessage>, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	private readonly IQueue<IQueuedMessage<TMessage>> _queue;
	private readonly MessageQueueContext<TMessage> _messageQueueContext;
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

	public QueueStatus QueueStatus { get; private set; }

	/// <inheritdoc/>
	public bool IsPull { get; }

	/// <inheritdoc/>
	public int? MaxSize { get; }

	/// <inheritdoc/>
	public TimeSpan? DefaultProcessingTimeout { get; }

	/// <inheritdoc/>
	public TimeSpan FetchInterval { get; set; }

	public HandleMessage<TMessage>? MessageHandler { get; }

	/// <inheritdoc/>
	public async Task<int> GetCountAsync(ITraceInfo traceInfo, ITransactionManagerFactory transactionManagerFactory, CancellationToken cancellationToken = default)
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));

		if (transactionManagerFactory == null)
			throw new ArgumentNullException(nameof(transactionManagerFactory));

		var transactionManager = transactionManagerFactory.Create();
		var transactionContext =
			await _messageQueueContext.ServiceBusOptions.TransactionContextFactory(
				_messageQueueContext.ServiceBusOptions.ServiceProvider,
				transactionManager).ConfigureAwait(false);

		var count = await TransactionInterceptor.ExecuteAsync(
			true,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var result = await _queue.GetCountAsync(traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				if (result.HasError)
					throw result.ToException()!;

				return result.Data;
			},
			nameof(GetCountAsync),
			async (traceInfo, exception, detail) =>
			{
				await _messageQueueContext.ServiceBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					_messageQueueContext.ServiceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exception),
					$"{nameof(GetCountAsync)} dispose {nameof(transactionContext)} error",
					null,
					cancellationToken: default).ConfigureAwait(false);
			},
			null,
			true,
			cancellationToken).ConfigureAwait(false);

		return count;
	}

	public MessageQueue(MessageQueueContext<TMessage> messageQueueContext)
	{
		_messageQueueContext = messageQueueContext ?? throw new ArgumentNullException(nameof(messageQueueContext));
		QueueName = _messageQueueContext.QueueName;
		QueueId = GuidConverter.ToGuid(QueueName);
		IsPull = _messageQueueContext.IsPull;
		QueueType = _messageQueueContext.QueueType;
		MaxSize = _messageQueueContext.MaxSize;

		if (QueueType == QueueType.Sequential_FIFO)
		{
			_queue = _messageQueueContext.FIFOQueue;
		}
		else if (QueueType == QueueType.Sequential_Delayable)
		{
			_queue = _messageQueueContext.DelayableQueue;
		}
		else
		{
			throw new NotImplementedException(QueueType.ToString());
		}

		DefaultProcessingTimeout = _messageQueueContext.DefaultProcessingTimeout;
		FetchInterval = _messageQueueContext.FetchInterval;
		MessageHandler = _messageQueueContext.MessageHandler;
	}

	/// <inheritdoc/>
	public async Task<IResult> EnqueueAsync(TMessage? message, IQueueEnqueueContext context, ITransactionContext transactionContext, CancellationToken cancellationToken)
	{
		var traceInfo = TraceInfo.Create(context.TraceInfo);
		var result = new ResultBuilder();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName}", new ObjectDisposedException(GetType().FullName));

		if (QueueStatus == QueueStatus.Terminated)
			return result.WithInvalidOperationException(traceInfo, $"{nameof(QueueStatus)} == {nameof(QueueStatus.Terminated)}");

		var queuedMessage = QueuedMessageFactory<TMessage>.CreateQueuedMessage(message, context, _messageQueueContext);

		var messagesMetadata = new List<IQueuedMessage<TMessage>> { queuedMessage };
		if (_messageQueueContext.MessageBodyProvider.AllowMessagePersistence(context.DisabledMessagePersistence, queuedMessage))
		{
			try
			{
				var saveResult =
					await _messageQueueContext.MessageBodyProvider.SaveToStorageAsync(
						messagesMetadata.Cast<IMessageMetadata>().ToList(),
						message,
						traceInfo,
						transactionContext,
						cancellationToken).ConfigureAwait(false);

				if (result.MergeHasError(saveResult))
					return await PublishQueueEventAsync(
						queuedMessage,
						traceInfo,
						QueueEventType.Enqueue,
						result.Build()).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				return await PublishQueueEventAsync(
						queuedMessage,
						traceInfo,
						QueueEventType.Enqueue,
						result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName}", ex)).ConfigureAwait(false);
			}
		}

		if (IsPull)
		{
			var enqueueResult = await _queue.EnqueueAsync(messagesMetadata, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(enqueueResult))
				return await PublishQueueEventAsync(queuedMessage, traceInfo, QueueEventType.Enqueue, result.Build()).ConfigureAwait(false);
		}
		else //IsPush
		{
			if (MessageHandler == null)
				return await PublishQueueEventAsync(
						queuedMessage,
						traceInfo,
						QueueEventType.Enqueue,
						result.WithInvalidOperationException(traceInfo, $"{nameof(IsPull)} = {IsPull} | {nameof(MessageHandler)} == null")).ConfigureAwait(false);

			if (context.IsAsynchronousInvocation)
			{
				var enqueueResult = await _queue.EnqueueAsync(messagesMetadata, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				if (result.MergeHasError(enqueueResult))
					return await PublishQueueEventAsync(queuedMessage, traceInfo, QueueEventType.Enqueue, result.Build()).ConfigureAwait(false);
			}
			else //is synchronous invocation
			{
				var handlerResult = await HandleMessageAsync(queuedMessage, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				result.MergeHasError(handlerResult);
				if (handlerResult.Data?.Processed == false)
					result.WithError(traceInfo, x => x.InternalMessage(handlerResult.Data.ToString()));
			}

			if (!result.HasError())
				context.OnMessageQueue = this;
		}

		return await PublishQueueEventAsync(queuedMessage, traceInfo, QueueEventType.Enqueue, result.Build()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IResult> TryRemoveAsync(IQueuedMessage<TMessage> message, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName}", new ObjectDisposedException(GetType().FullName));

		if (IsPull)
			return await PublishQueueEventAsync(
				message,
				traceInfo,
				QueueEventType.Remove,
				(IResult)result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName} | {nameof(TryRemoveAsync)}: {nameof(IsPull)} = {IsPull}")).ConfigureAwait(false);

		var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
		result.MergeHasError(removeResult);
		return await PublishQueueEventAsync(
			message,
			traceInfo,
			QueueEventType.Remove,
			(IResult)result.Build()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IResult<IQueuedMessage<TMessage>?>> TryPeekAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IQueuedMessage<TMessage>?>();

		if (disposed)
			return result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName}", new ObjectDisposedException(GetType().FullName));

		var peekResult = await _queue.TryPeekAsync(traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
		if (result.MergeHasError(peekResult))
			return await PublishQueueEventAsync(null, traceInfo, QueueEventType.Peek, result.Build()).ConfigureAwait(false);

		var messageHeader = peekResult.Data;

		if (messageHeader == null)
			return await PublishQueueEventAsync(
				messageHeader,
				traceInfo,
				QueueEventType.Peek,
				result.Build()).ConfigureAwait(false);

		if (messageHeader is not IQueuedMessage<TMessage> queuedMessage)
			return await PublishQueueEventAsync(
					messageHeader,
					traceInfo,
					QueueEventType.Peek,
					result.WithInvalidOperationException(
						traceInfo,
						$"QueueName = {_messageQueueContext.QueueName} | {nameof(_queue)} must by type of {typeof(IQueuedMessage<TMessage>).FullName} but {messageHeader.GetType().FullName} found.")).ConfigureAwait(false);

		if (QueueType == QueueType.Sequential_FIFO && (queuedMessage.MessageStatus == MessageStatus.Suspended || queuedMessage.MessageStatus == MessageStatus.Aborted))
		{
			QueueStatus = QueueStatus.Suspended;
			//if (!IsPull)
			//{
				return await PublishQueueEventAsync(
					null,
					traceInfo,
					QueueEventType.Peek,
					result.Build()).ConfigureAwait(false);
			//}
		}

		if (_messageQueueContext.MessageBodyProvider.AllowMessagePersistence(queuedMessage.DisabledMessagePersistence, queuedMessage))
		{
			try
			{
				var loadResult = await _messageQueueContext.MessageBodyProvider.LoadFromStorageAsync<TMessage>(queuedMessage, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				if (result.MergeHasError(loadResult))
					return await PublishQueueEventAsync(messageHeader, traceInfo, QueueEventType.Peek, result.Build()).ConfigureAwait(false);

				var message = loadResult.Data;

				//kedze plati ContainsContent = true
				if (message == null)
					return await PublishQueueEventAsync(
						messageHeader,
						traceInfo,
						QueueEventType.Peek,
						result.WithInvalidOperationException(
							traceInfo,
							$"QueueName = {_messageQueueContext.QueueName} | {nameof(TryPeekAsync)}: {nameof(queuedMessage.QueueName)} == {queuedMessage.QueueName} | {nameof(queuedMessage.MessageId)} == {queuedMessage.MessageId} | {nameof(message)} == null")).ConfigureAwait(false);

				queuedMessage.SetMessage(message);
			}
			catch (Exception ex)
			{
				return await PublishQueueEventAsync(
					messageHeader,
					traceInfo,
					QueueEventType.Peek,
					result.WithInvalidOperationException(traceInfo, $"QueueName = {_messageQueueContext.QueueName}", ex)).ConfigureAwait(false);
			}
		}

		return await PublishQueueEventAsync(
			messageHeader,
			traceInfo,
			QueueEventType.Peek,
			result.WithData(queuedMessage).Build()).ConfigureAwait(false);
	}

	Task IMessageQueue.OnMessageAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
		=> OnMessageAsync(traceInfo, cancellationToken);

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

			var transactionManagerFactory = _messageQueueContext.ServiceBusOptions.TransactionManagerFactory;
			while (0 < (await GetCountAsync(traceInfo, transactionManagerFactory, cancellationToken).ConfigureAwait(false)))
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				var transactionManager = _messageQueueContext.ServiceBusOptions.TransactionManagerFactory.Create();
				var transactionContext =
					await _messageQueueContext.ServiceBusOptions.TransactionContextFactory(
						_messageQueueContext.ServiceBusOptions.ServiceProvider,
						transactionManager).ConfigureAwait(false);

				IQueuedMessage<TMessage>? message = null;

				var loopControl = await TransactionInterceptor.ExecuteAsync(
					false,
					traceInfo,
					transactionContext,
					//$"{nameof(message.QueueName)} == {message?.QueueName} | {nameof(message.SourceExchangeName)} == {message?.SourceExchangeName} | MessageType = {message?.Message?.GetType().FullName}"
					async (traceInfo, transactionContext, cancellationToken) =>
					{
						var peekResult = await TryPeekAsync(traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
						if (peekResult.HasError)
						{
							await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(peekResult, null, cancellationToken).ConfigureAwait(false);
							transactionContext.ScheduleRollback(nameof(TryPeekAsync));

							return LoopControlEnum.Return;
						}

						message = peekResult.Data;

						if (message != null)
						{
							if (message.Processed)
							{
								var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
								if (removeResult.HasError)
								{
									transactionContext.ScheduleRollback(nameof(_queue.TryRemoveAsync));

									await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken).ConfigureAwait(false);
									await PublishQueueEventAsync(message, traceInfo, QueueEventType.OnMessage, removeResult).ConfigureAwait(false);
								}
								else
								{
									transactionContext.ScheduleCommit();
								}

								return LoopControlEnum.Continue;
							}

							var nowUtc = DateTime.UtcNow;
							if (message.TimeToLiveUtc < nowUtc)
							{
								if (!message.DisableFaultQueue)
								{
									try
									{
										var faultContext = _messageQueueContext.ServiceBusOptions.QueueProvider.CreateFaultQueueContext(traceInfo, message);
										var enqueueResult = await _messageQueueContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync(message.Message, faultContext, transactionContext, cancellationToken).ConfigureAwait(false);

										if (enqueueResult.HasError)
										{
											transactionContext.ScheduleRollback($"{nameof(_messageQueueContext.ServiceBusOptions.ExchangeProvider.FaultQueue)}.{nameof(_messageQueueContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync)}");
										}
										else
										{
											transactionContext.ScheduleCommit();
										}
									}
									catch (Exception faultEx)
									{
										await _messageQueueContext.ServiceBusOptions.HostLogger.LogErrorAsync(
											traceInfo,
											_messageQueueContext.ServiceBusOptions.HostInfo,
											HostStatus.Unchanged,
											x => x
												.ExceptionInfo(faultEx)
												.Detail($"{nameof(message.QueueName)} == {message.QueueName} | {nameof(message.SourceExchangeName)} == {message.SourceExchangeName} | MessageType = {message.Message?.GetType().FullName} >> {nameof(_messageQueueContext.ServiceBusOptions.QueueProvider.FaultQueue)}.{nameof(_messageQueueContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync)}"),
											$"{nameof(OnMessageAsync)} >> {nameof(_messageQueueContext.ServiceBusOptions.QueueProvider.FaultQueue)}",
											null,
											cancellationToken: default).ConfigureAwait(false);
									}
								}

								return LoopControlEnum.Continue;
							}

							var handlerResult = await HandleMessageAsync(message, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
							if (handlerResult.Data!.Processed)
							{
								var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
								if (removeResult.HasError)
								{
									transactionContext.ScheduleRollback($"{nameof(HandleMessageAsync)} - {nameof(_queue.TryRemoveAsync)}");

									await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken).ConfigureAwait(false);
									await PublishQueueEventAsync(message, traceInfo, QueueEventType.OnMessage, removeResult).ConfigureAwait(false);
								}
								else
								{
									transactionContext.ScheduleCommit();
								}
							}
						}

						return LoopControlEnum.None;
					},
					$"{nameof(OnMessageAsync)} {nameof(message.Processed)} = {message?.Processed}",
					async (traceInfo, exception, detail) =>
					{
						await _messageQueueContext.ServiceBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						_messageQueueContext.ServiceBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

						await PublishQueueEventAsync(
							message,
							traceInfo,
							QueueEventType.OnMessage,
							new ResultBuilder().WithInvalidOperationException(traceInfo, "Global exception!", exception)).ConfigureAwait(false);
					},
					null,
					true,
					cancellationToken).ConfigureAwait(false);

				if (loopControl == LoopControlEnum.Break || loopControl == LoopControlEnum.Return)
					break;
			}
		}
	}

	private async Task<IResult<IMessageMetadataUpdate>> HandleMessageAsync(IQueuedMessage<TMessage> message, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IMessageMetadataUpdate>();

		var processingTimeout = DefaultProcessingTimeout;

		var handlerContext = _messageQueueContext.ServiceBusOptions.MessageHandlerContextFactory(_messageQueueContext.ServiceBusOptions.ServiceProvider);
		handlerContext.ServiceBusOptions = _messageQueueContext.ServiceBusOptions;
		handlerContext.MessageHandlerResultFactory = _messageQueueContext.ServiceBusOptions.MessageHandlerResultFactory;
		handlerContext.TransactionContext = transactionContext;
		handlerContext.ServiceProvider = _messageQueueContext.ServiceBusOptions.ServiceProvider;
		handlerContext.TraceInfo = TraceInfo.Create(traceInfo);
		handlerContext.HostInfo = _messageQueueContext.ServiceBusOptions.HostInfo;
		handlerContext.HandlerLogger = _messageQueueContext.ServiceBusOptions.HandlerLogger;
		handlerContext.MessageId = message.MessageId;
		handlerContext.DisabledMessagePersistence = message.DisabledMessagePersistence;
		handlerContext.ThrowNoHandlerException = true;
		handlerContext.PublisherId = PublisherHelper.GetPublisherIdentifier(_messageQueueContext.ServiceBusOptions.HostInfo, traceInfo);
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
		handlerContext.HasSelfContent = message.HasSelfContent;
		handlerContext.Priority = message.Priority;
		handlerContext.Headers = message.Headers;

		handlerContext.Initialize(message.MessageStatus, message.DelayedToUtc);

		var task = MessageHandler!(message, handlerContext, cancellationToken);
		if (processingTimeout.HasValue)
			task = task.OrTimeoutAsync(processingTimeout.Value);

		MessageHandlerResult? handlerResult = null;

		try
		{
			handlerResult = await task.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			result.WithInvalidOperationException(traceInfo, $"{nameof(handlerResult)} == null", ex);
			await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken).ConfigureAwait(false);
		}

		if (handlerResult == null)
		{
			result.WithInvalidOperationException(traceInfo, $"{nameof(handlerResult)} == null");

			await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken).ConfigureAwait(false);
			return await PublishQueueEventAsync(message, traceInfo, QueueEventType.OnMessage, result.Build()).ConfigureAwait(false);
		}

		var hasError = handlerResult.ErrorResult?.HasError == true;
		if (hasError)
		{
			result.MergeAllHasError(handlerResult.ErrorResult!);
			await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken).ConfigureAwait(false);
			await PublishQueueEventAsync(message, traceInfo, QueueEventType.OnMessage, result.Build()).ConfigureAwait(false);
		}

		var update = new MessageMetadataUpdate(message.MessageId)
		{
			MessageStatus = handlerResult.MessageStatus
		};

		if (update.MessageStatus != MessageStatus.Completed)
		{
			if (handlerResult.Retry)
			{
				var errorController = message.ErrorHandling ?? _messageQueueContext.ErrorHandling;
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

		if (QueueType == QueueType.Sequential_FIFO && (update.MessageStatus == MessageStatus.Suspended || update.MessageStatus == MessageStatus.Aborted) && QueueStatus != QueueStatus.Terminated)
			QueueStatus = QueueStatus.Suspended;

		var localTransactionManager = _messageQueueContext.ServiceBusOptions.TransactionManagerFactory.Create();
		var localTransactionContext =
			await _messageQueueContext.ServiceBusOptions.TransactionContextFactory(
				_messageQueueContext.ServiceBusOptions.ServiceProvider,
				localTransactionManager).ConfigureAwait(false);

		var queueStatus = await TransactionInterceptor.ExecuteAsync(
			false,
			traceInfo,
			localTransactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var updateResult = await _queue.UpdateAsync(message, update, traceInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				if (updateResult.HasError)
				{
					transactionContext.ScheduleRollback();
				}
				else
				{
					transactionContext.ScheduleCommit();
				}

				return updateResult.Data;
			},
			$"{nameof(HandleMessageAsync)} {nameof(_queue.UpdateAsync)} | {nameof(message.MessageId)} = {message.MessageId}",
			(traceInfo, exception, detail) =>
			{
				return _messageQueueContext.ServiceBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					_messageQueueContext.ServiceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exception).Detail(detail),
					detail,
					null,
					cancellationToken: default);
			},
			null,
			true,
			cancellationToken).ConfigureAwait(false);

		if (QueueStatus == QueueStatus.Running)
			QueueStatus = queueStatus;
		else if (queueStatus == QueueStatus.Terminated)
			QueueStatus = queueStatus;

		return await PublishQueueEventAsync(
			message,
			traceInfo,
			QueueEventType.OnMessage,
			result.WithData(update).Build()).ConfigureAwait(false);
	}

	private async Task<TResult> PublishQueueEventAsync<TResult>(IMessageMetadata? message, ITraceInfo traceInfo, QueueEventType queueEventType, TResult result)
		where TResult : IResult
	{
		IQueueEvent queueEvent;
		if (result.HasError)
		{
			queueEvent = new QueueErrorEvent(this, queueEventType, message, result);
			await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result, null, cancellationToken: default).ConfigureAwait(false);
		}
		else
		{
			queueEvent = new QueueEvent(this, queueEventType, message);
			await _messageQueueContext.ServiceBusOptions.HostLogger.LogResultAllMessagesAsync(result, null, cancellationToken: default).ConfigureAwait(false);
		}

		await _messageQueueContext.ServiceBusOptions.ServiceBusLifeCycleEventManager.PublishServiceBusEventInternalAsync(
			queueEvent,
			traceInfo,
			_messageQueueContext.ServiceBusOptions).ConfigureAwait(false);

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
