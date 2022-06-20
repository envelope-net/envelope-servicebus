using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Model;

public interface IServiceBusEvent : IEvent
{
	IMessageMetadata? Message { get; }
}
