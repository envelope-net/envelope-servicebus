using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Orchestrations;

public interface IOrchestrationRepository : IOrchestrationEventQueue
{
	Task CreateNewOrchestrationAsync(IOrchestrationInstance orchestration, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task UpdateOrchestrationStatusAsync(Guid idOrchestrationInstance, OrchestrationStatus status, DateTime? completeTimeUtc, ITransactionContext transactionContext);

	Task AddFinalizedBranchAsync(Guid idOrchestrationInstance, IOrchestrationStep finalizedBranch, ITransactionContext transactionContext, CancellationToken cancellationToken = default);
	
	Task<List<Guid>> GetFinalizedBrancheIdsAsync(Guid idOrchestrationInstance, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	//TODO: dorobit (ak treba) internal Task<bool> SaveOrchestrationDataAsync();

	//Task<IEnumerable<Guid>> GetRunnableInstancesAsync(DateTime nowUtc, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(
		Guid idOrchestrationInstance,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionContext transactionContext,
		CancellationToken cancellationToken = default);

	Task<List<IOrchestrationInstance>> GetOrchestrationInstancesAsync(
		string orchestrationKey,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionContext transactionContext,
		CancellationToken cancellationToken = default);

	public Task<List<IOrchestrationInstance>> GetAllUnfinishedOrchestrationInstancesAsync(
		Guid idOrchestrationDefinition,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionContext transactionContext,
		CancellationToken cancellationToken = default);

	Task<bool?> IsCompletedOrchestrationAsync(Guid idOrchestrationInstance, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task AddExecutionPointerAsync(ExecutionPointer executionPointer, ITransactionContext transactionContext);

	Task AddNestedExecutionPointerAsync(ExecutionPointer executionPointer, ExecutionPointer parentExecutionPointer, ITransactionContext transactionContext);

	Task<List<ExecutionPointer>> GetOrchestrationExecutionPointersAsync(Guid idOrchestrationInstance, ITransactionContext transactionContext);

	Task<ExecutionPointer?> GetStepExecutionPointerAsync(Guid idOrchestrationInstance, Guid idStep, ITransactionContext transactionContext);

	Task UpdateExecutionPointerAsync(ExecutionPointer executionPointer, IExecutionPointerUpdate update, ITransactionContext transactionContext);
}
