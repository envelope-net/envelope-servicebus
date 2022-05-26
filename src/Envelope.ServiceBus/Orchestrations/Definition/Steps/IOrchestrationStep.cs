using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps;

public interface IOrchestrationStep : IValidable
{
	Guid IdStep { get; }

	Type? BodyType { get; }

	string Name { get; internal set; }

	bool IsRootStep { get; internal set; }

	IOrchestrationDefinition OrchestrationDefinition { get; internal set; }

	IOrchestrationStep? NextStep { get; internal set; }

	IReadOnlyDictionary<object, IOrchestrationStep> Branches { get; }

	IOrchestrationStep? BranchController { get; internal set; }

	IOrchestrationStep? StartingStep { get; internal set; }

	bool IsStartingStep { get; }

	IErrorHandlingController? ErrorHandlingController { get; internal set; }

	TimeSpan? DistributedLockExpiration { get; internal set; }

	IStepBody? ConstructBody(IServiceProvider serviceProvider);

	IErrorHandlingController? GetErrorHandlingController();

	bool CanRetry(int retryCount);

	TimeSpan? GetRetryInterval(int retryCount);

	internal AssignParameters? SetInputParameters { get; set; }

	internal AssignParameters? SetOutputParameters { get; set; }
}
