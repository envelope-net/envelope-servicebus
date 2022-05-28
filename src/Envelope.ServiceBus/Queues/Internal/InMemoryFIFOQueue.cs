using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Queues.Internal;

internal class InMemoryFIFOQueue : IQueue, IDisposable
{
	private readonly ConcurrentQueue<IMessageMetadata> _messages;

	private bool disposed;

	public int Count => _messages.Count;

	/// <inheritdoc/>
	public int? MaxSize { get; set; }

	public InMemoryFIFOQueue(int? maxSize = null)
	{
		_messages = new ConcurrentQueue<IMessageMetadata>();
		MaxSize = maxSize;
	}

	private readonly object _enqueueLock = new();
	/// <inheritdoc/>
	public Task<IResult> EnqueueAsync(List<IMessageMetadata> messagesMetadata, ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<Guid>();

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
	public Task<IResult> TryRemoveAsync(IMessageMetadata messageMetadata, ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<Guid>();

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
	public Task<IResult<IMessageMetadata?>> TryPeekAsync(ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<IMessageMetadata?>();
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
