using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps;

public interface IOrchestrationStep : IValidable
{
	Guid IdStep { get; }

	Type? BodyType { get; }

	string Name { get; set; }

	bool IsRootStep { get; set; }

	IOrchestrationDefinition OrchestrationDefinition { get; set; }

	IOrchestrationStep? NextStep { get; set; }

	IReadOnlyDictionary<object, IOrchestrationStep> Branches { get; }

	IOrchestrationStep? BranchController { get; set; }

	IOrchestrationStep? StartingStep { get; set; }

	bool IsStartingStep { get; }

	IErrorHandlingController? ErrorHandlingController { get; set; }

	TimeSpan? DistributedLockExpiration { get; set; }

	IStepBody? ConstructBody(IServiceProvider serviceProvider);

	IErrorHandlingController? GetErrorHandlingController();

	bool CanRetry(int retryCount);

	TimeSpan? GetRetryInterval(int retryCount);

	AssignParameters? SetInputParametersInternal { get; set; }

	AssignParameters? SetOutputParametersInternal { get; set; }
}
