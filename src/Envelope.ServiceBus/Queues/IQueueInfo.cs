using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues;

public interface IQueueInfo
{
	Guid QueueId { get; }

	string QueueName { get; }

	/// <summary>
	/// If true, te queue is an <see cref="IFaultQueue"/>
	/// </summary>
	bool IsFaultQueue { get; }

	/// <summary>
	/// If true, the messages survive queue restart,
	/// else not.
	/// </summary>
	bool IsPersistent { get; }

	/// <summary>
	/// Sequntial or Parallel queue
	/// </summary>
	QueueType QueueType { get; }

	QueueStatus QueueStatus { get; }

	/// <summary>
	/// Queue's max size
	/// </summary>
	int? MaxSize { get; }


	/// <summary>
	///  Gets the number of elements contained in the queue.
	/// </summary>
	Task<int> GetCountAsync(ITraceInfo traceInfo, ITransactionManagerFactory transactionManagerFactory, CancellationToken cancellationToken = default);

}
