using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Queues;

public interface IMessageQueue : IQueueInfo, IDisposable, IAsyncDisposable
{
	/// <summary>
	/// If true, messages are waiting until the subscribers pick them up,
	/// else the queue push the messages to subscribers
	/// </summary>
	bool IsPull { get; }

	/// <summary>
	/// The timespan after which the message processing will be cancelled.
	/// </summary>
	TimeSpan? DefaultProcessingTimeout { get; }

	/// <summary>
	/// Fetch messages interval for push queue
	/// </summary>
	TimeSpan FetchInterval { get; set; }
}

public interface IMessageQueue<TMessage> : IMessageQueue, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	HandleMessage<TMessage>? MessageHandler { get; }

	/// <summary>
	/// Enqueue the new message
	/// </summary>
	Task<IResult<Guid>> EnqueueAsync(TMessage? message, IQueueEnqueueContext context, CancellationToken cancellationToken);

	/// <summary>
	/// If the queue is pull queue than the subscriber call this method to try to return an message from
	/// the beginning of the queue without removing it.
	/// </summary>
	Task<IResult<IQueuedMessage<TMessage>?, Guid>> TryPeekAsync(ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);

	/// <summary>
	/// If the queue is pull queue than the subscriber call this method to try to remove and return the message
	/// at the beginning of the queue.
	/// </summary>
	Task<IResult<Guid>> TryRemoveAsync(IQueuedMessage<TMessage> message, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);
}
