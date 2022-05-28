using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Model;

public interface IExchangeErrorEvent : IExchangeEvent, IServiceBusEvent, IEvent
{
	IResult ErrorResult { get; }
}
