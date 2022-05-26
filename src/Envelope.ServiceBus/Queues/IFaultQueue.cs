using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Queues;

public interface IFaultQueue : IQueueInfo
{
	/// <summary>
	/// Enqueue the new message
	/// </summary>
	Task<IResult<Guid>> EnqueueAsync(IMessage? message, IFaultQueueContext context, CancellationToken cancellationToken);
}
