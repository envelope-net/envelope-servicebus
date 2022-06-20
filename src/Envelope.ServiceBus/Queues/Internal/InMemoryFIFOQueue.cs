using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Queues.Internal;

internal class InMemoryFIFOQueue<T> : IQueue<T>, IDisposable
	where T : IMessageMetadata
{
	private readonly ConcurrentQueue<T> _messages;

	private bool disposed;

	/// <inheritdoc/>
	public int? MaxSize { get; set; }

	public InMemoryFIFOQueue(int? maxSize = null)
	{
		_messages = new ConcurrentQueue<T>();
		MaxSize = maxSize;
	}

	public Task<IResult<int>> GetCountAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
		=> Task.FromResult(new ResultBuilder<int>().WithData(_messages.Count).Build());

	private readonly object _enqueueLock = new();
	/// <inheritdoc/>
	public Task<IResult> EnqueueAsync(List<T> messagesMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder();

		if (messagesMetadata == null)
			return Task.FromResult((IResult)result.WithArgumentNullException(traceInfo, nameof(messagesMetadata)));

		if (messagesMetadata.Count == 0)
			return Task.FromResult((IResult)result.Build());

		if (_messages.Count == MaxSize)
			return Task.FromResult((IResult)result.WithInvalidOperationException(traceInfo, $"Max size exceeded. Count = {MaxSize}"));

		if (messagesMetadata.Count == 1)
		{
			_messages.Enqueue(messagesMetadata[0]);
		}
		else
		{
			lock (_enqueueLock)
			{
				foreach (var messageHeader in messagesMetadata)
					_messages.Enqueue(messageHeader);
			}
		}

		return Task.FromResult((IResult)result.Build());
	}

	/// <inheritdoc/>
	public Task<IResult> TryRemoveAsync(T messageMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder();

		if (messageMetadata == null)
			return Task.FromResult((IResult)result.WithArgumentNullException(traceInfo, nameof(messageMetadata)));

		//_messages.TryPeek(out var messageHeader);
		//if (messageHeader != null && messageHeader.MessageId != message.MessageId)
		//	return result.WithInvalidOperationException(traceInfo, $"Expected {nameof(message.MessageId)} = {message.MessageId}, but root {nameof(messageHeader.MessageId)} == {messageHeader.MessageId}");

		//_messages.TryDequeue(out var msg);
		//if (msg != null && msg.MessageId != messageMetadata.MessageId)
		//	return result.WithInvalidOperationException(
		//		traceInfo,
		//		$"Expected {nameof(messageMetadata.MessageId)} = {messageMetadata.MessageId}, but root {nameof(msg.MessageId)} == {msg.MessageId}");

		_messages.TryDequeue(out var _);

		return Task.FromResult((IResult)result.Build());
	}

	/// <inheritdoc/>
	public Task<IResult<QueueStatus>> UpdateAsync(T messageMetadata, IMessageMetadataUpdate update, ITraceInfo traceInfo, ITransactionContext localTransactionContext, CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<QueueStatus>();

		if (messageMetadata == null)
			return Task.FromResult(result.WithArgumentNullException(traceInfo, nameof(messageMetadata)));

		if (update == null)
			return Task.FromResult(result.WithArgumentNullException(traceInfo, nameof(update)));

		if (messageMetadata.MessageId != update.MessageId)
			return Task.FromResult(result.WithInvalidOperationException(traceInfo, $"{nameof(messageMetadata.MessageId)} != {nameof(update)}.{nameof(update.MessageId)} | {messageMetadata.MessageId} != {update.MessageId}"));

		messageMetadata.Update(update.Processed, update.MessageStatus, update.RetryCount, update.DelayedToUtc);

		return
			Task.FromResult(
				result
					.WithData((update.MessageStatus == MessageStatus.Suspended || update.MessageStatus == MessageStatus.Aborted)
						? QueueStatus.Suspended
						: QueueStatus.Running)
					.Build());
	}

	/// <inheritdoc/>
	public Task<IResult<T?>> TryPeekAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<T?>();
		_messages.TryPeek(out var messageMetadata);
		return Task.FromResult(result.WithData(messageMetadata).Build());
	}

	public void Clear()
		=> _messages.Clear();

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				Clear();
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
