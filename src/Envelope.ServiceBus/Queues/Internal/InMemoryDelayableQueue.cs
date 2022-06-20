using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues.Internal;

internal class InMemoryDelayableQueue<T> : IQueue<T>, IDisposable
	where T : IMessageMetadata
{
	private readonly object _lock = new();

	private bool disposed;
	private int _size;
	private T[] _messages;

	/// <inheritdoc/>
	public int? MaxSize { get; set; }

	public InMemoryDelayableQueue(int? maxSize = null)
	{
		_messages = Array.Empty<T>();
		MaxSize = maxSize;
	}

	public Task<IResult<int>> GetCountAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
		=> Task.FromResult(new ResultBuilder<int>().WithData(_size).Build());

	/// <inheritdoc/>
	public Task<IResult> EnqueueAsync(List<T> messagesMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder();

		if (messagesMetadata == null)
			return Task.FromResult((IResult)result.WithArgumentNullException(traceInfo, nameof(messagesMetadata)));

		if (messagesMetadata.Count == 0)
			return Task.FromResult((IResult)result.Build());

		if (_messages.Length == MaxSize)
			return Task.FromResult((IResult)result.WithInvalidOperationException(traceInfo, $"Max size exceeded. Count = {MaxSize}"));

		lock (_lock)
		{
			if (_messages.Length == MaxSize)
				return Task.FromResult((IResult)result.WithInvalidOperationException(traceInfo, $"Max size exceeded. Count = {MaxSize}"));

			var currentSize = _size;
			_size += messagesMetadata.Count;

			if (_messages.Length <= _size)
				Grow(_size);

			for (int i = 0; i < messagesMetadata.Count; i++)
				_messages[currentSize + i] = messagesMetadata[i];
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

		if (_size == 0)
			return Task.FromResult((IResult)result.Build());

		lock (_lock)
		{
			if (_size == 0)
				return Task.FromResult((IResult)result.Build());

			var index = -1;
			for (int i = 0; i < _size; i++)
			{
				var node = _messages[i];
				if (node.MessageId == messageMetadata.MessageId)
				{
					index = i;
					break;
				}
			}

			if (0 <= index)
			{
				int lastNodeIndex = --_size;

				if (index < lastNodeIndex)
					Array.Copy(_messages, index + 1, _messages, index, lastNodeIndex - index);

				_messages[lastNodeIndex] = default!;
			}
		}

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

		T? messageMetadata = default;

		if (_size == 0)
			return Task.FromResult(result.WithData(messageMetadata).Build());

		lock (_lock)
		{
			if (0 < _size)
				messageMetadata = _messages[0];

			return Task.FromResult(result.WithData(messageMetadata).Build());
		}
	}

	private void Grow(int minCapacity)
	{
		const int GrowFactor = 2;
		const int MinimumGrow = 4;

		int newcapacity = GrowFactor * _messages.Length;

		// Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
		// Note that this check works even when _messages.Length overflowed thanks to the (uint) cast
		if (Array.MaxLength < (uint)newcapacity)
			newcapacity = Array.MaxLength;

		// Ensure minimum growth is respected.
		newcapacity = Math.Max(newcapacity, _messages.Length + MinimumGrow);

		// If the computed capacity is still less than specified, set to the original argument.
		// Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
		if (newcapacity < minCapacity)
			newcapacity = minCapacity;

		Array.Resize(ref _messages, newcapacity);
	}

	public void Clear()
	{
		lock (_lock)
		{
			_messages = Array.Empty<T>();
			_size = 0;
		}
	}

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
