using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IOrchestrationTerminated : ILifeCycleEvent, IEvent
{
}
