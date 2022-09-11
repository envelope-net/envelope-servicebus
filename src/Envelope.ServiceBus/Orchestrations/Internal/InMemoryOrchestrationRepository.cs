using Envelope.Extensions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Orchestrations.Internal;

internal class InMemoryOrchestrationRepository : IOrchestrationRepository, IOrchestrationEventQueue
{
	private static readonly ConcurrentDictionary<Guid, IOrchestrationInstance> _instances = new(); //ConcurrentDictionary<IdOrchestrationInstance, IOrchestrationInstance>
	private static readonly ConcurrentDictionary<string, List<IOrchestrationInstance>> _instancesByKey = new(); //ConcurrentDictionary<OrchestrationKey, IOrchestrationInstance>
	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, OrchestrationEvent>> _eventsQueue = new(); //ConcurrentDictionary<OrchestrationKey, ConcurrentDictionary<event.IdMessage, OrchestrationEvent>>
	private static readonly ConcurrentDictionary<Guid, List<ExecutionPointer>> _executionPointers = new();  //ConcurrentDictionary<IdOrchestrationInstance, List<ExecutionPointer>>
	private static readonly ConcurrentDictionary<Guid, List<IOrchestrationStep>> _finalizedBranches = new();  //ConcurrentDictionary<IdOrchestrationInstance, List<IOrchestrationStep>>

	public Task CreateNewOrchestrationAsync(IOrchestrationInstance orchestration, ITransactionController transactionController, CancellationToken cancellationToken = default)
	{
		if (orchestration == null)
			throw new ArgumentNullException(nameof(orchestration));

		_instances.AddOrUpdate(
			orchestration.IdOrchestrationInstance,
			key =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, new List<IOrchestrationInstance> { orchestration }, (key, list) => { list.AddUniqueItem(orchestration); return list; });
				return orchestration;
			},
			(key, value) =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, new List<IOrchestrationInstance> { orchestration }, (key, list) => { list.AddUniqueItem(orchestration); return list; });
				return orchestration;
			});
		return Task.CompletedTask;
	}

	//public Task<IEnumerable<Guid>> GetRunnableInstancesAsync(DateTime nowUtc, ITransactionController transactionController, CancellationToken cancellationToken = default)
	//{
	//	var now = nowUtc.Ticks;
	//	return Task.FromResult((IEnumerable<Guid>)_instances
	//		.ToDictionary(x => x.Key, x => x.Value)
	//		//.Where(x => x.Value.NextExecutionTimerTicks.HasValue && x.Value.NextExecutionTimerTicks <= now)
	//		.Select(x => x.Value.IdOrchestrationInstance)
	//		.ToList());
	//}

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(
		Guid idOrchestrationInstance,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default)
	{
		_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance);
		return Task.FromResult(orchestrationInstance);
	}

	public Task<List<IOrchestrationInstance>> GetOrchestrationInstancesAsync(
		string orchestrationKey,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default)
	{
		_instancesByKey.TryGetValue(orchestrationKey, out var orchestrationInstances);
		return Task.FromResult(orchestrationInstances ?? new List<IOrchestrationInstance>());
	}

	public Task<List<IOrchestrationInstance>> GetAllUnfinishedOrchestrationInstancesAsync(
		Guid idOrchestrationDefinition,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_instances.Values.Where(x =>
			x.IdOrchestrationDefinition == idOrchestrationDefinition
			&& (x.Status == OrchestrationStatus.Running || x.Status == OrchestrationStatus.Executing)).ToList());
	}

	public Task<bool?> IsCompletedOrchestrationAsync(Guid idOrchestrationInstance, ITransactionController transactionController, CancellationToken cancellationToken = default)
	{
		if (_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
		{
			if (!_executionPointers.TryGetValue(orchestrationInstance.IdOrchestrationInstance, out var pointers))
				throw new InvalidOperationException($"No orchestration with {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance} found");

			var completed = pointers.All(x => x.Status == PointerStatus.Completed);

			return Task.FromResult((bool?)completed);
		}

		return Task.FromResult((bool?)null);
	}

	public Task AddExecutionPointerAsync(ExecutionPointer executionPointer, ITransactionController transactionController)
	{
		if (executionPointer == null)
			throw new ArgumentNullException(nameof(executionPointer));

		_executionPointers.AddOrUpdate(
			executionPointer.IdOrchestrationInstance,
			id => new List<ExecutionPointer> { executionPointer },
			(id, list) =>
			{
				list.AddUniqueItem(executionPointer);
				return list;
			});

		return Task.CompletedTask;
	}

	public Task AddNestedExecutionPointerAsync(ExecutionPointer executionPointer, ExecutionPointer parentExecutionPointer, ITransactionController transactionController)
	{
		if (executionPointer == null)
			throw new ArgumentNullException(nameof(executionPointer));

		_executionPointers.AddOrUpdate(
			executionPointer.IdOrchestrationInstance,
			id => new List<ExecutionPointer> { executionPointer },
			(id, list) =>
			{
				list.AddUniqueItem(executionPointer);
				return list;
			});

		return Task.CompletedTask;
	}

	public Task<List<ExecutionPointer>> GetOrchestrationExecutionPointersAsync(Guid idOrchestrationInstance, ITransactionController transactionController)
	{
		if (!_executionPointers.TryGetValue(idOrchestrationInstance, out var pointers))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		return Task.FromResult(pointers);
	}

	public Task<ExecutionPointer?> GetStepExecutionPointerAsync(Guid idOrchestrationInstance, Guid idStep, ITransactionController transactionController)
	{
		if (!_executionPointers.TryGetValue(idOrchestrationInstance, out var pointers))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		var pointer = pointers.Where(x => x.IdStep == idStep).FirstOrDefault();
		return Task.FromResult(pointer);
	}

	public Task UpdateExecutionPointerAsync(ExecutionPointer executionPointer, IExecutionPointerUpdate update, ITransactionController transactionController)
	{
		if (executionPointer == null)
			throw new ArgumentNullException(nameof(executionPointer));

		if (update == null)
			throw new ArgumentNullException(nameof(update));

		if (executionPointer.IdExecutionPointer != update.IdExecutionPointer)
			throw new InvalidOperationException($"{nameof(executionPointer.IdExecutionPointer)} != {nameof(update)}.{nameof(update.IdExecutionPointer)} | {executionPointer.IdExecutionPointer} != {update.IdExecutionPointer}");

		executionPointer.Update(update);

		return Task.CompletedTask;
	}

	public Task UpdateOrchestrationStatusAsync(Guid idOrchestrationInstance, OrchestrationStatus status, DateTime? completeTimeUtc, ITransactionController transactionController)
		=> Task.CompletedTask;

	public Task AddFinalizedBranchAsync(Guid idOrchestrationInstance, IOrchestrationStep finalizedBranch, ITransactionController transactionController, CancellationToken cancellationToken = default)
	{
		_finalizedBranches.AddOrUpdate(
			idOrchestrationInstance,
			id => new List<IOrchestrationStep> { finalizedBranch },
			(id, list) =>
			{
				list.AddUniqueItem(finalizedBranch);
				return list;
			});

		return Task.CompletedTask;
	}

	public Task<List<Guid>> GetFinalizedBrancheIdsAsync(Guid idOrchestrationInstance, ITransactionController transactionController, CancellationToken cancellationToken = default)
	{
		_finalizedBranches.TryGetValue(idOrchestrationInstance, out var finalizedBranches);
		return Task.FromResult(finalizedBranches?.Select(x => x.IdStep).ToList() ?? new List<Guid>());
	}











	public Task<IResult> SaveNewEventAsync(OrchestrationEvent @event, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder();

		if (@event == null)
			return Task.FromResult((IResult)result.WithArgumentNullException(traceInfo, nameof(@event)));

		var instanceEventsDict = _eventsQueue.GetOrAdd(@event.OrchestrationKey, key => new ConcurrentDictionary<Guid, OrchestrationEvent>());

		instanceEventsDict.TryAdd(@event.Id, @event);
		return Task.FromResult((IResult)result.Build());
	}

	public Task<IResult<List<OrchestrationEvent>?>> GetUnprocessedEventsAsync(string orchestrationKey, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<OrchestrationEvent>?>();

		if (string.IsNullOrWhiteSpace(orchestrationKey))
			return Task.FromResult(result.WithArgumentNullException(traceInfo, nameof(orchestrationKey)));

		if (_eventsQueue.TryGetValue(orchestrationKey, out var instanceEventsDict))
			result.WithData(instanceEventsDict.Values.Where(x => !x.ProcessedUtc.HasValue).ToList());

		return Task.FromResult(result.Build());
	}

	public Task<IResult> SetProcessedUtcAsync(OrchestrationEvent @event, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder();

		if (@event == null)
			return Task.FromResult((IResult)result.WithArgumentNullException(traceInfo, nameof(@event)));

		if (_eventsQueue.TryGetValue(@event.OrchestrationKey, out var instanceEventsDict)
			&& instanceEventsDict.TryGetValue(@event.Id, out var existingEvent))
		{
			existingEvent.ProcessedUtc = @event.ProcessedUtc;
			//instanceEventsDict.TryUpdate(@event.Id, @event, existingEvent);
		}
		else
			return Task.FromResult((IResult)result.WithInvalidOperationException(traceInfo, $"Nothing to update | {nameof(@event.OrchestrationKey)} = {@event.OrchestrationKey} | {nameof(@event.Id)} = {@event.Id}"));

		return Task.FromResult((IResult)result.Build());
	}
}
