using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Model;

public interface IExchangeEvent : IServiceBusEvent, IEvent
{
	DateTime EventTimeUtc { get; }

	ExchangeEventType ExchangeEventType { get; }

	IExchange Exchange { get; }
}

public enum ExchangeEventType
{
	Enqueue,
	OnMessage,
	Peek,
	Remove
}
