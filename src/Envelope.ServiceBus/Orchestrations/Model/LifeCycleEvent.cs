using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public abstract class LifeCycleEvent : ILifeCycleEvent, IEvent
{
	public DateTime EventTimeUtc { get; }

	public IOrchestrationInstance OrchestrationInstance { get; }

	public LifeCycleEvent(IOrchestrationInstance orchestrationInstance)
	{
		EventTimeUtc = DateTime.UtcNow;
		OrchestrationInstance = orchestrationInstance ?? throw new ArgumentNullException(nameof(orchestrationInstance));
	}
}
