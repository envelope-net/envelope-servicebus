using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class StepCompleted : StepLifeCycleEvent, IStepCompleted, IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	public StepCompleted(IOrchestrationInstance orchestrationInstance, IExecutionPointer executionPointer)
		: base(orchestrationInstance, executionPointer)
	{
	}
}
