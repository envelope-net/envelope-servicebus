using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;

namespace Envelope.ServiceBus.Model.Internal;

internal class QueueEvent : IQueueEvent, IServiceBusEvent, IEvent
{
	public IMessageMetadata? Message { get; }

	public DateTime EventTimeUtc { get; set; }

	public QueueEventType QueueEventType { get; }

	public IMessageQueue MessageQueue { get; set; }

	public QueueEvent(IMessageQueue messageQueue, QueueEventType queueEventType, IMessageMetadata? message)
	{
		EventTimeUtc = DateTime.UtcNow;
		QueueEventType = queueEventType;
		MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
		Message = message;
	}
}
