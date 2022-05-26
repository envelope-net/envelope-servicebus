using Envelope.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model.Internal;

internal class OrchestrationError : StepLifeCycleEvent, IOrchestrationError, IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	public IErrorMessage<Guid> ErrorMessage { get; set; }

	public OrchestrationError(IOrchestrationInstance orchestrationInstance, IExecutionPointer executionPointer, IErrorMessage<Guid> errorMessage)
		: base(orchestrationInstance, executionPointer)
	{
		ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
	}
}
