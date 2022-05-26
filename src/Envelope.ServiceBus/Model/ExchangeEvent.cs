using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Model;

public class ExchangeEvent : IExchangeEvent, IServiceBusEvent, IEvent
{
	public DateTime EventTimeUtc { get; set; }

	public ExchangeEventType ExchangeEventType { get; }

	public IExchange Exchange { get; set; }

	public ExchangeEvent(IExchange exchange, ExchangeEventType exchangeEventType)
	{
		EventTimeUtc = DateTime.UtcNow;
		ExchangeEventType = exchangeEventType;
		Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
	}
}
