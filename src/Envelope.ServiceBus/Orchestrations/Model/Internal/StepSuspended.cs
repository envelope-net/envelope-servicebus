using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class StepSuspended : StepLifeCycleEvent, IStepSuspended, IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	public StepSuspended(IOrchestrationInstance orchestrationInstance, IExecutionPointer executionPointer)
		: base(orchestrationInstance, executionPointer)
	{
	}
}
