using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class OrchestrationTerminated : LifeCycleEvent, IOrchestrationTerminated, ILifeCycleEvent, IEvent
{
	public OrchestrationTerminated(IOrchestrationInstance orchestrationInstance)
		: base(orchestrationInstance)
	{
	}
}
