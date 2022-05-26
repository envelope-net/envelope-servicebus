using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations;

public interface IOrchestrationInstance : IDistributedLockKeyFactory, IDisposable, IAsyncDisposable
{
	Guid IdOrchestrationInstance { get; }
	
	string OrchestrationKey { get; }

	IOrchestrationDefinition OrchestrationDefinition { get; }

	bool IsSingleton { get; }

	IReadOnlyExecutionPointerCollection ExecutionPointers { get; }

	IReadOnlyList<IOrchestrationStep> FinalizedBranches { get; }

	OrchestrationStatus Status { get; internal set; }

	int Version { get; }

	object Data { get; }

	DateTime CreateTimeUtc { get; }

	DateTime? CompleteTimeUtc { get; }

	TimeSpan WorkerIdleTimeout { get; }

	internal void AddExecutionPointer(ExecutionPointer executionPointer);

	internal void AddFinalizedBranch(IOrchestrationStep step);

	internal ExecutionPointer? GetStepExecutionPointer(Guid idStep);

	internal void UpdateOrchestrationStatus(OrchestrationStatus status, DateTime? completeTimeUtc = null);

	internal Task<bool> StartOrchestrationWorkerAsync();
}
