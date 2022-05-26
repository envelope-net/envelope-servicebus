using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Persistence;

public interface IOrchestrationRepository : IOrchestrationEventQueue
{
	Task CreateNewOrchestrationAsync(IOrchestrationInstance orchestration, CancellationToken cancellationToken = default);

	Task UpdateOrchestrationStatusAsync(Guid idOrchestrationInstance, OrchestrationStatus status, DateTime? completeTimeUtc = null);

	Task AddFinalizedBranchAsync(Guid idOrchestrationInstance, IOrchestrationStep finalizedBranch, CancellationToken cancellationToken = default);

	//TODO: dorobit (ak treba) internal Task<bool> SaveOrchestrationDataAsync();

	Task<IEnumerable<Guid>> GetRunnableInstancesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

	Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default);

	Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(string orchestrationKey, CancellationToken cancellationToken = default);

	Task AddExecutionPointerAsync(Guid idOrchestrationInstance, ExecutionPointer executionPointer);

	Task AddNestedExecutionPointerAsync(Guid idOrchestrationInstance, ExecutionPointer executionPointer, ExecutionPointer parentExecutionPointer);

	Task<ExecutionPointer?> GetStepExecutionPointerAsync(Guid idOrchestrationInstance, Guid idStep);

	Task UpdateExecutionPointerAsync(ExecutionPointer executionPointer);
}
