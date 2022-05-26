using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;
using Envelope.Services;

namespace Envelope.ServiceBus.Model.Internal;

internal class QueueErrorEvent : QueueEvent, IQueueErrorEvent, IQueueEvent, IServiceBusEvent, IEvent
{
	public IResult<Guid> ErrorResult { get; }

	public QueueErrorEvent(IMessageQueue messageQueue, QueueEventType queueEventType, IResult<Guid> errorResult)
		: base(messageQueue, queueEventType)
	{
		ErrorResult = errorResult ?? throw new ArgumentNullException(nameof(errorResult));
	}
}
