using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues;

public interface IFaultQueue : IQueueInfo
{
	/// <summary>
	/// Enqueue the new message
	/// </summary>
	Task<IResult> EnqueueAsync(IMessage? message, IFaultQueueContext context, ITransactionContext transactionContext, CancellationToken cancellationToken);
}
