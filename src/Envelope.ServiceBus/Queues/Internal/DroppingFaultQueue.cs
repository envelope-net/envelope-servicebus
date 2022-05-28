using Envelope.Converters;
using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Queues.Internal;

internal class DroppingFaultQueue : IFaultQueue
{
	public Guid QueueId { get; }

	public string QueueName { get; }

	public bool IsFaultQueue => true;

	public bool IsPersistent => false;

	public long Count => 0;

	public int? MaxSize => null;

	public QueueType QueueType => QueueType.Parallel;

	public Task<IResult> EnqueueAsync(IMessage? message, IFaultQueueContext context, CancellationToken cancellationToken)
		=> Task.FromResult((IResult)new ResultBuilder<Guid>().Build());

	public DroppingFaultQueue()
	{
		QueueName = typeof(DroppingFaultQueue).FullName!;
		QueueId = GuidConverter.ToGuid(QueueName);
	}
}
