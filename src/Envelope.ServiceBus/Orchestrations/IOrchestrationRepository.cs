using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Orchestrations;

public interface IOrchestrationRepository : IOrchestrationEventQueue
{
	Task CreateNewOrchestrationAsync(IOrchestrationInstance orchestration, ITransactionController transactionController, CancellationToken cancellationToken = default);

	Task UpdateOrchestrationStatusAsync(Guid idOrchestrationInstance, OrchestrationStatus status, DateTime? completeTimeUtc, ITransactionController transactionController);

	Task AddFinalizedBranchAsync(Guid idOrchestrationInstance, IOrchestrationStep finalizedBranch, ITransactionController transactionController, CancellationToken cancellationToken = default);
	
	Task<List<Guid>> GetFinalizedBrancheIdsAsync(Guid idOrchestrationInstance, ITransactionController transactionController, CancellationToken cancellationToken = default);

	//TODO: dorobit (ak treba) internal Task<bool> SaveOrchestrationDataAsync();

	//Task<IEnumerable<Guid>> GetRunnableInstancesAsync(DateTime nowUtc, ITransactionController transactionController, CancellationToken cancellationToken = default);

	Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(
		Guid idOrchestrationInstance,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default);

	Task<List<IOrchestrationInstance>> GetOrchestrationInstancesAsync(
		string orchestrationKey,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default);

	public Task<List<IOrchestrationInstance>> GetAllUnfinishedOrchestrationInstancesAsync(
		Guid idOrchestrationDefinition,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default);

	Task<bool?> IsCompletedOrchestrationAsync(Guid idOrchestrationInstance, ITransactionController transactionController, CancellationToken cancellationToken = default);

	Task AddExecutionPointerAsync(ExecutionPointer executionPointer, ITransactionController transactionController);

	Task AddNestedExecutionPointerAsync(ExecutionPointer executionPointer, ExecutionPointer parentExecutionPointer, ITransactionController transactionController);

	Task<List<ExecutionPointer>> GetOrchestrationExecutionPointersAsync(Guid idOrchestrationInstance, ITransactionController transactionController);

	Task<ExecutionPointer?> GetStepExecutionPointerAsync(Guid idOrchestrationInstance, Guid idStep, ITransactionController transactionController);

	Task UpdateExecutionPointerAsync(ExecutionPointer executionPointer, IExecutionPointerUpdate update, ITransactionController transactionController);
}
