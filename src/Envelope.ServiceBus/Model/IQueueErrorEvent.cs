using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Model;

public interface IQueueErrorEvent : IQueueEvent, IServiceBusEvent, IEvent
{
	IResult<Guid> ErrorResult { get; }
}
