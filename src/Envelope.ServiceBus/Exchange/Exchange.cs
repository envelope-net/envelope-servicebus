using Envelope.Converters;
using Envelope.Extensions;
using Envelope.Infrastructure;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Model;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Threading;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Exchange;

public class Exchange<TMessage> : IExchange<TMessage>, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	private readonly IQueue<IExchangeMessage<TMessage>> _queue;
	private readonly ExchangeContext<TMessage> _exchangeContext;
	private bool _disposed;

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

	public QueueStatus QueueStatus { get; private set; }

	/// <inheritdoc/>
	public int? MaxSize { get; }

	/// <inheritdoc/>
	public Exchange(ExchangeContext<TMessage> exchangeContext)
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

	protected virtual ITransactionController CreateTransactionController()
		=> _exchangeContext.ServiceBusOptions.ServiceProvider.GetRequiredService<ITransactionCoordinator>().TransactionController;

	/// <inheritdoc/>
	public async Task<int> GetCountAsync(ITraceInfo traceInfo, CancellationToken cancellationToken = default)
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));

		var transactionController = CreateTransactionController();

		var count = await TransactionInterceptor.ExecuteAsync(
			true,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, cancellationToken) =>
			{
				var result = await _queue.GetCountAsync(traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
				if (result.HasError)
					throw result.ToException()!;

				return result.Data;
			},
			nameof(GetCountAsync),
			async (traceInfo, exception, detail) =>
			{
				await _exchangeContext.ServiceBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					_exchangeContext.ServiceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exception),
					nameof(GetCountAsync),
					null,
					cancellationToken: default).ConfigureAwait(false);
			},
			null,
			true,
			cancellationToken).ConfigureAwait(false);

		return count;
	}

	/// <inheritdoc/>
	public async Task<IResult<List<Guid>>> EnqueueAsync(TMessage? message, IExchangeEnqueueContext context, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		var traceInfo = TraceInfo.Create(context.TraceInfo);
		var result = new ResultBuilder<List<Guid>>();

		if (_disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		if (QueueStatus == QueueStatus.Terminated)
			return result.WithInvalidOperationException(traceInfo, $"{nameof(QueueStatus)} == {nameof(QueueStatus.Terminated)}");

		var createResult = _exchangeContext.ExchangeMessageFactory.CreateExchangeMessages(message, context, _exchangeContext, traceInfo);
		if (result.MergeHasError(createResult))
			return await PublishExchangeEventAsync(null, traceInfo, ExchangeEventType.Enqueue, result.Build()).ConfigureAwait(false);

		var exchangeMessages = createResult.Data;
		if (exchangeMessages == null || exchangeMessages.Count == 0)
			return await PublishExchangeEventAsync(
				null,
				traceInfo,
				ExchangeEventType.Enqueue,
				result.WithInvalidOperationException(traceInfo, $"No exchange messages was created for {typeof(TMessage).FullName} | ExchangeName = {_exchangeContext.ExchangeName}")).ConfigureAwait(false);

		if (!context.IsAsynchronousInvocation && 1 < exchangeMessages.Count)
			return await PublishExchangeEventAsync(
				null,
				traceInfo,
				ExchangeEventType.Enqueue,
				result.WithInvalidOperationException(traceInfo, $"Multiple messages for synchronous invocation is not allowed. MessageType = {typeof(TMessage).FullName} | ExchangeName = {_exchangeContext.ExchangeName}")).ConfigureAwait(false);

		var exchangeMessage = exchangeMessages[0];
		if (_exchangeContext.MessageBodyProvider.AllowAnyMessagePersistence(context.DisabledMessagePersistence, exchangeMessages))
		{
			try
			{
				var saveResult =
					await _exchangeContext.MessageBodyProvider.SaveToStorageAsync(
						exchangeMessages.Cast<IMessageMetadata>().ToList(),
						message,
						traceInfo,
						transactionController,
						cancellationToken).ConfigureAwait(false);

				if (result.MergeHasError(saveResult))
					return await PublishExchangeEventAsync(exchangeMessages.Count == 1 ? exchangeMessage : null, traceInfo, ExchangeEventType.Enqueue, result.Build()).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				return await PublishExchangeEventAsync(
					exchangeMessages.Count == 1 ? exchangeMessage : null,
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", ex)).ConfigureAwait(false);
			}
		}

		if (context.IsAsynchronousInvocation)
		{
			var enqueueResult = await _queue.EnqueueAsync(exchangeMessages, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(enqueueResult))
				return await PublishExchangeEventAsync(exchangeMessages.Count == 1 ? exchangeMessage : null, traceInfo, ExchangeEventType.Enqueue, result.Build()).ConfigureAwait(false);

			context.CallExchangeOnMessage = true;

			return
				await PublishExchangeEventAsync(
					exchangeMessages.Count == 1 ? exchangeMessage : null,
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithData(exchangeMessages.Select(x => x.MessageId).ToList()).Build()).ConfigureAwait(false);
		}
		else
		{
			var brokerResult = await _exchangeContext.MessageBrokerHandler.HandleAsync(exchangeMessage, _exchangeContext, transactionController, cancellationToken).ConfigureAwait(false);

			if (brokerResult.ErrorResult?.HasError == true)
				result.MergeHasError(brokerResult.ErrorResult!);

			var handlerResult = await ProcessMessageHandlerResultAsync(exchangeMessage, traceInfo, brokerResult, transactionController, cancellationToken).ConfigureAwait(false);
			result.MergeHasError(handlerResult);
			if (handlerResult.Data?.Processed == false)
				result.WithError(traceInfo, x => x.InternalMessage(handlerResult.Data.ToString()));

			context.OnMessageQueue = brokerResult.OnMessageQueue;

			return
				await PublishExchangeEventAsync(
					exchangeMessage,
					traceInfo,
					ExchangeEventType.Enqueue,
					result.WithData(new List<Guid> { exchangeMessage.MessageId }).Build()).ConfigureAwait(false);
		}
	}

	/// <inheritdoc/>
	public async Task<IResult> TryRemoveAsync(IExchangeMessage<TMessage> message, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder();

		if (_disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
		result.MergeHasError(removeResult);
		return
			await PublishExchangeEventAsync(
				message,
				traceInfo,
				ExchangeEventType.Remove,
				(IResult)result.Build()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IResult<IExchangeMessage<TMessage>?>> TryPeekAsync(ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IExchangeMessage<TMessage>?>();

		if (_disposed)
			return result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", new ObjectDisposedException(GetType().FullName));

		var peekResult = await _queue.TryPeekAsync(traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
		if (result.MergeHasError(peekResult))
			return await PublishExchangeEventAsync(null, traceInfo, ExchangeEventType.Peek, result.Build()).ConfigureAwait(false);

		var messageHeader = peekResult.Data;

		if (messageHeader == null)
			return await PublishExchangeEventAsync(messageHeader, traceInfo, ExchangeEventType.Peek, result.Build()).ConfigureAwait(false);

		if (messageHeader is not IExchangeMessage<TMessage> exchangeMessage)
			return await PublishExchangeEventAsync(
					messageHeader,
					traceInfo,
					ExchangeEventType.Peek,
					result.WithInvalidOperationException(
						traceInfo,
						$"ExchangeName = {_exchangeContext.ExchangeName} | {nameof(_queue)} must by type of {typeof(IExchangeMessage<TMessage>).FullName} but {messageHeader.GetType().FullName} found.")).ConfigureAwait(false);

		if (QueueType == QueueType.Sequential_FIFO && (exchangeMessage.MessageStatus == MessageStatus.Suspended || exchangeMessage.MessageStatus == MessageStatus.Aborted))
		{
			QueueStatus = QueueStatus.Suspended;

			return await PublishExchangeEventAsync(
				null,
				traceInfo,
				ExchangeEventType.Peek,
				result.Build()).ConfigureAwait(false);
		}

		if (_exchangeContext.MessageBodyProvider.AllowMessagePersistence(exchangeMessage.DisabledMessagePersistence, exchangeMessage))
		{
			try
			{
				var loadResult = await _exchangeContext.MessageBodyProvider.LoadFromStorageAsync<TMessage>(exchangeMessage, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
				if (result.MergeHasError(loadResult))
					return await PublishExchangeEventAsync(messageHeader, traceInfo, ExchangeEventType.Peek, result.Build()).ConfigureAwait(false);

				var message = loadResult.Data;

				//kedze plati ContainsContent = true
				if (message == null)
					return await PublishExchangeEventAsync(
						messageHeader,
						traceInfo,
						ExchangeEventType.Peek,
						result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName} | {nameof(TryPeekAsync)}: {nameof(exchangeMessage.ExchangeName)} == {exchangeMessage.ExchangeName} | {nameof(exchangeMessage.MessageId)} == {exchangeMessage.MessageId} | {nameof(message)} == null")).ConfigureAwait(false);

				exchangeMessage.SetMessage(message);
			}
			catch (Exception ex)
			{
				return await PublishExchangeEventAsync(
					messageHeader,
					traceInfo,
					ExchangeEventType.Peek,
					result.WithInvalidOperationException(traceInfo, $"ExchangeName = {_exchangeContext.ExchangeName}", ex)).ConfigureAwait(false);
			}
		}

		return await PublishExchangeEventAsync(
			messageHeader,
			traceInfo,
			ExchangeEventType.Peek,
			result.WithData(exchangeMessage).Build()).ConfigureAwait(false);
	}

	Task IExchange.OnMessageAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
		=> OnMessageAsync(traceInfo, cancellationToken);

	private readonly AsyncLock _onMessageLock = new();
	private async Task OnMessageAsync(ITraceInfo traceInfo, CancellationToken cancellationToken)
	{
		if (_disposed || _exchangeContext.ServiceBusOptions.ServiceBusMode == ServiceBusMode.PublishOnly)
			return;

		traceInfo = TraceInfo.Create(traceInfo);

		using (await _onMessageLock.LockAsync().ConfigureAwait(false))
		{
			if (_disposed)
				return;

			while (0 < (await GetCountAsync(traceInfo, cancellationToken).ConfigureAwait(false)))
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				var transactionController = CreateTransactionController();

				IExchangeMessage<TMessage>? message = null;
				IMessageQueue? onMessageQueue = null;

				var loopControl = await TransactionInterceptor.ExecuteAsync(
					false,
					traceInfo,
					transactionController,
					//$"{nameof(message.ExchangeName)} == {message?.ExchangeName} | {nameof(message.TargetQueueName)} == {message?.TargetQueueName} | MessageType = {message?.Message?.GetType().FullName}"
					async (traceInfo, transactionController, cancellationToken) =>
					{
						var peekResult = await TryPeekAsync(traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
						if (peekResult.HasError)
						{
							await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(peekResult, null, cancellationToken).ConfigureAwait(false);
							transactionController.ScheduleRollback(nameof(TryPeekAsync));
							return LoopControlEnum.Return;
						}

						message = peekResult.Data;

						if (message != null)
						{
							if (message.Processed)
							{
								var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
								if (removeResult.HasError)
								{
									transactionController.ScheduleRollback(nameof(_queue.TryRemoveAsync));

									await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken).ConfigureAwait(false);
									await PublishExchangeEventAsync(message, traceInfo, ExchangeEventType.OnMessage, removeResult).ConfigureAwait(false);
								}
								else
								{
									transactionController.ScheduleCommit();
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
										var faultContext = _exchangeContext.ServiceBusOptions.QueueProvider.CreateFaultQueueContext(traceInfo, message);
										var enqueueResult = await _exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync(message.Message, faultContext, transactionController, cancellationToken).ConfigureAwait(false);

										if (enqueueResult.HasError)
										{
											transactionController.ScheduleRollback($"{nameof(_exchangeContext.ServiceBusOptions.ExchangeProvider.FaultQueue)}.{nameof(_exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync)}");
										}
										else
										{
											transactionController.ScheduleCommit();
										}
									}
									catch (Exception faultEx)
									{
										await _exchangeContext.ServiceBusOptions.HostLogger.LogErrorAsync(
											traceInfo,
											_exchangeContext.ServiceBusOptions.HostInfo,
											HostStatus.Unchanged,
											x => x
												.ExceptionInfo(faultEx)
												.Detail($"{nameof(message.ExchangeName)} == {message.ExchangeName} | {nameof(message.TargetQueueName)} == {message.TargetQueueName} | MessageType = {message.Message?.GetType().FullName} >> {nameof(_exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue)}.{nameof(_exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync)}"),
											$"{nameof(OnMessageAsync)} >> {nameof(_exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue)}",
											null,
											cancellationToken: default).ConfigureAwait(false);
									}
								}

								return LoopControlEnum.Continue;
							}

							var brokerResult = await _exchangeContext.MessageBrokerHandler.HandleAsync(message, _exchangeContext, transactionController, cancellationToken).ConfigureAwait(false);

							var processResult = await ProcessMessageHandlerResultAsync(message, traceInfo, brokerResult, null, cancellationToken).ConfigureAwait(false);
							if (processResult.Data!.Processed)
							{
								var removeResult = await _queue.TryRemoveAsync(message, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
								if (removeResult.HasError)
								{
									transactionController.ScheduleRollback($"{nameof(ProcessMessageHandlerResultAsync)} - {nameof(_queue.TryRemoveAsync)}");

									await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(removeResult, null, cancellationToken).ConfigureAwait(false);
									await PublishExchangeEventAsync(message, traceInfo, ExchangeEventType.OnMessage, removeResult).ConfigureAwait(false);
								}
								else
								{
									transactionController.ScheduleCommit();
									onMessageQueue = brokerResult.OnMessageQueue;
								}
							}
						}

						return LoopControlEnum.None;
					},
					$"{nameof(OnMessageAsync)} {nameof(message.Processed)} = {message?.Processed}",
					async (traceInfo, exception, detail) =>
					{
						await _exchangeContext.ServiceBusOptions.HostLogger.LogErrorAsync(
							traceInfo,
							_exchangeContext.ServiceBusOptions.HostInfo,
							HostStatus.Unchanged,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

						await PublishExchangeEventAsync(
							message,
							traceInfo,
							ExchangeEventType.OnMessage,
							new ResultBuilder().WithInvalidOperationException(traceInfo, "Global exception!", exception)).ConfigureAwait(false);
					},
					null,
					true,
					cancellationToken).ConfigureAwait(false);

				if (loopControl == LoopControlEnum.Break || loopControl == LoopControlEnum.Return)
					break;

				if (onMessageQueue != null)
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await onMessageQueue.OnMessageAsync(TraceInfo.Create(traceInfo), cancellationToken: default).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							await _exchangeContext.ServiceBusOptions.HostLogger.LogErrorAsync(
								TraceInfo.Create(traceInfo),
								_exchangeContext.ServiceBusOptions.HostInfo,
								HostStatus.Unchanged,
								x => x.ExceptionInfo(ex),
								$"{nameof(OnMessageAsync)} >> {nameof(onMessageQueue)}",
								null,
								cancellationToken: default).ConfigureAwait(false);
						}

					},
					cancellationToken: default);
				}
			}
		}
	}

	private async Task<IResult<IMessageMetadataUpdate>> ProcessMessageHandlerResultAsync(
		IExchangeMessage<TMessage> message,
		ITraceInfo traceInfo,
		MessageHandlerResult brokerResult,
		ITransactionController? transactionController,
		CancellationToken cancellationToken)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IMessageMetadataUpdate>();

		var hasError = brokerResult.ErrorResult?.HasError == true;

		if (hasError)
		{
			result.MergeAllHasError(brokerResult.ErrorResult!);
			await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result.Build(), null, cancellationToken).ConfigureAwait(false);
			await PublishExchangeEventAsync(message, traceInfo, ExchangeEventType.OnMessage, result.Build()).ConfigureAwait(false);
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

		if (QueueType == QueueType.Sequential_FIFO && (update.MessageStatus == MessageStatus.Suspended || update.MessageStatus == MessageStatus.Aborted) && QueueStatus != QueueStatus.Terminated)
			QueueStatus = QueueStatus.Suspended;

		bool isLocalTransactionController = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionController = true;
		}

		var queueStatus = await TransactionInterceptor.ExecuteAsync(
			false,
			traceInfo,
			transactionController,
			async (traceInfo, transactionController, cancellationToken) =>
			{
				var updateResult = await _queue.UpdateAsync(message, update, traceInfo, transactionController, cancellationToken).ConfigureAwait(false);
				if (updateResult.HasError)
				{
					transactionController.ScheduleRollback();
				}
				else
				{
					transactionController.ScheduleCommit();
				}

				return updateResult.Data;
			},
			$"{nameof(ProcessMessageHandlerResultAsync)}<{typeof(TMessage).FullName}>",
			(traceInfo, exception, detail) =>
			{
				return _exchangeContext.ServiceBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					_exchangeContext.ServiceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exception).Detail(detail),
					detail,
					null,
					cancellationToken: default);
			},
			null,
			isLocalTransactionController,
			cancellationToken).ConfigureAwait(false);

		if (QueueStatus == QueueStatus.Running)
			QueueStatus = queueStatus;
		else if (queueStatus == QueueStatus.Terminated)
			QueueStatus = queueStatus;

		return await PublishExchangeEventAsync(
			message,
			traceInfo,
			ExchangeEventType.OnMessage,
			result.WithData(update).Build()).ConfigureAwait(false);
	}

	private async Task<TResult> PublishExchangeEventAsync<TResult>(IMessageMetadata? message, ITraceInfo traceInfo, ExchangeEventType exchangeEventType, TResult result)
		where TResult : IResult
	{
		IExchangeEvent exchangeEvent;
		if (result.HasError)
		{
			exchangeEvent = new ExchangeErrorEvent(this, exchangeEventType, message, result);
			await _exchangeContext.ServiceBusOptions.HostLogger.LogResultErrorMessagesAsync(result, null, cancellationToken: default).ConfigureAwait(false);
		}
		else
		{
			exchangeEvent = new ExchangeEvent(this, exchangeEventType, message);
			await _exchangeContext.ServiceBusOptions.HostLogger.LogResultAllMessagesAsync(result, null, cancellationToken: default).ConfigureAwait(false);
		}

		await _exchangeContext.ServiceBusOptions.ServiceBusLifeCycleEventManager.PublishServiceBusEventInternalAsync(
			exchangeEvent,
			traceInfo,
			_exchangeContext.ServiceBusOptions).ConfigureAwait(false);

		return result;
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;

		_disposed = true;

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
		if (_disposed)
			return;

		_disposed = true;

		if (disposing)
			_queue.Dispose();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
