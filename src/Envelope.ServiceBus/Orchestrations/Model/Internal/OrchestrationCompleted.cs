using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class OrchestrationCompleted : LifeCycleEvent, IOrchestrationCompleted, ILifeCycleEvent, IEvent
{
	public OrchestrationCompleted(IOrchestrationInstance orchestrationInstance)
		: base(orchestrationInstance)
	{
	}
}
