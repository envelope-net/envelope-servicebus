using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IStepCompleted : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
}
