using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class StepStarted : StepLifeCycleEvent, IStepStarted, IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	public StepStarted(IOrchestrationInstance orchestrationInstance, IExecutionPointer executionPointer)
		: base(orchestrationInstance, executionPointer)
	{
	}
}
