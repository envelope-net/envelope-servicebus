using Envelope.Converters;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues.Internal;

internal class DroppingFaultQueue : IFaultQueue, IQueueInfo
{
	public Guid QueueId { get; }

	public string QueueName { get; }

	public bool IsFaultQueue => true;

	public bool IsPersistent => false;

	public int? MaxSize => null;

	public QueueType QueueType => QueueType.Parallel;

	public QueueStatus QueueStatus => QueueStatus.Running;

	public DroppingFaultQueue()
	{
		QueueName = typeof(DroppingFaultQueue).FullName!;
		QueueId = GuidConverter.ToGuid(QueueName);
	}

	public Task<IResult> EnqueueAsync(IMessage? message, IFaultQueueContext context, ITransactionController transactionController, CancellationToken cancellationToken)
		=> Task.FromResult((IResult)new ResultBuilder().Build());

	public Task<int> GetCountAsync(ITraceInfo traceInfo, CancellationToken cancellationToken = default)
		=> Task.FromResult(0);
}
