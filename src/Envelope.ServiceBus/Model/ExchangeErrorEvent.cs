using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Model;

public class ExchangeErrorEvent : ExchangeEvent, IExchangeErrorEvent, IExchangeEvent, IServiceBusEvent, IEvent
{
	public IResult ErrorResult { get; }

	public ExchangeErrorEvent(IExchange exchange, ExchangeEventType exchangeEventType, IResult errorResult)
		: base(exchange, exchangeEventType)
	{
		ErrorResult = errorResult ?? throw new ArgumentNullException(nameof(errorResult));
	}
}
