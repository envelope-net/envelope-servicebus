using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IStepSuspended : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
}
