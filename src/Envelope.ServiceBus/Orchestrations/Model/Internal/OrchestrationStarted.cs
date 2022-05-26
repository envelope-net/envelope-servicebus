using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class OrchestrationStarted : LifeCycleEvent, IOrchestrationStarted, ILifeCycleEvent, IEvent
{
	public OrchestrationStarted(IOrchestrationInstance orchestrationInstance)
		: base(orchestrationInstance)
	{
	}
}
