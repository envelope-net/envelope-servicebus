using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model;

public abstract class StepLifeCycleEvent : LifeCycleEvent, IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	public IExecutionPointer ExecutionPointer { get; }

	protected StepLifeCycleEvent(IOrchestrationInstance orchestrationInstance, IExecutionPointer executionPointer)
		: base(orchestrationInstance)
	{
		ExecutionPointer = executionPointer ?? throw new ArgumentNullException(nameof(executionPointer));
	}
}
