using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
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
