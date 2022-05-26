using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Model;

public class ExchangeErrorEvent : ExchangeEvent, IExchangeErrorEvent, IExchangeEvent, IServiceBusEvent, IEvent
{
	public IResult<Guid> ErrorResult { get; }

	public ExchangeErrorEvent(IExchange exchange, ExchangeEventType exchangeEventType, IResult<Guid> errorResult)
		: base(exchange, exchangeEventType)
	{
		ErrorResult = errorResult ?? throw new ArgumentNullException(nameof(errorResult));
	}
}
