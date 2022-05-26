using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IStepCompleted : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
}
