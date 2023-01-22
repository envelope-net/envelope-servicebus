using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IQueueErrorEvent : IQueueEvent, IServiceBusEvent, IEvent
{
	IResult ErrorResult { get; }
}
