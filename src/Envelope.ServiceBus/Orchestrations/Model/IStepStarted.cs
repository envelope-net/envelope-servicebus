using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IStepStarted : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
}
