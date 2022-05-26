using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class OrchestrationResumed : LifeCycleEvent, IOrchestrationResumed, ILifeCycleEvent, IEvent
{
	public OrchestrationResumed(IOrchestrationInstance orchestrationInstance)
		: base(orchestrationInstance)
	{
	}
}
