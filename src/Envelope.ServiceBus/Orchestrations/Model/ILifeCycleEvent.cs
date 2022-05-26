using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface ILifeCycleEvent : IEvent
{
	DateTime EventTimeUtc { get; }

	IOrchestrationInstance OrchestrationInstance { get; }
}
