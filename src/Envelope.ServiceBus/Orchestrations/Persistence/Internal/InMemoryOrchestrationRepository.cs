using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Orchestrations.Persistence.Internal;

internal class InMemoryOrchestrationRepository : IOrchestrationRepository, IOrchestrationEventQueue
{
	private static readonly ConcurrentDictionary<Guid, IOrchestrationInstance> _instances = new();
	private static readonly ConcurrentDictionary<string, IOrchestrationInstance> _instancesByKey = new();
	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, OrchestrationEvent>> _eventsQueue = new(); //ConcurrentDictionary<OrchestrationKey, ConcurrentDictionary<event.IdMessage, OrchestrationEvent>>

	public Task CreateNewOrchestrationAsync(IOrchestrationInstance orchestration, CancellationToken cancellationToken = default)
	{
		if (orchestration == null)
			throw new ArgumentNullException(nameof(orchestration));

		_instances.AddOrUpdate(
			orchestration.IdOrchestrationInstance,
			key =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, orchestration, (key, value) => orchestration);
				return orchestration;
			},
			(key, value) =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, orchestration, (key, value) => orchestration);
				return orchestration;
			});
		return Task.CompletedTask;
	}

	public Task SaveOrchestrationAsync(IOrchestrationInstance orchestration, CancellationToken cancellationToken = default)
	{
		if (orchestration == null)
			throw new ArgumentNullException(nameof(orchestration));

		_instances.AddOrUpdate(
			orchestration.IdOrchestrationInstance,
			key =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, orchestration, (key, value) => orchestration);
				return orchestration;
			},
			(key, value) =>
			{
				_instancesByKey.AddOrUpdate(orchestration.OrchestrationKey, orchestration, (key, value) => orchestration);
				return orchestration;
			});
		return Task.CompletedTask;
	}

	public Task<IEnumerable<Guid>> GetRunnableInstancesAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
	{
		var now = nowUtc.Ticks;
		return Task.FromResult((IEnumerable<Guid>)_instances
			.ToDictionary(x => x.Key, x => x.Value)
			//.Where(x => x.Value.NextExecutionTimerTicks.HasValue && x.Value.NextExecutionTimerTicks <= now)
			.Select(x => x.Value.IdOrchestrationInstance)
			.ToList());
	}

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken)
	{
		_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance);
		return Task.FromResult(orchestrationInstance);
	}

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(string orchestrationKey, CancellationToken cancellationToken)
	{
		_instancesByKey.TryGetValue(orchestrationKey, out var orchestrationInstance);
		return Task.FromResult(orchestrationInstance);
	}

	public Task AddExecutionPointerAsync(Guid idOrchestrationInstance, ExecutionPointer executionPointer)
	{
		if (!_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		orchestrationInstance.AddExecutionPointer(executionPointer);
		return Task.CompletedTask;
	}

	public Task AddNestedExecutionPointerAsync(Guid idOrchestrationInstance, ExecutionPointer executionPointer, ExecutionPointer parentExecutionPointer)
	{
		if (!_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		orchestrationInstance.AddExecutionPointer(executionPointer);
		return Task.CompletedTask;
	}

	public Task<ExecutionPointer?> GetStepExecutionPointerAsync(Guid idOrchestrationInstance, Guid idStep)
	{
		if (!_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		var pointer = orchestrationInstance.GetStepExecutionPointer(idStep);
		return Task.FromResult(pointer);
	}

	public Task UpdateExecutionPointerAsync(ExecutionPointer executionPointer)
		=> Task.CompletedTask;

	public Task UpdateOrchestrationStatusAsync(Guid idOrchestrationInstance, OrchestrationStatus status, DateTime? completeTimeUtc = null)
	{
		if (!_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		orchestrationInstance.UpdateOrchestrationStatus(status, completeTimeUtc);
		return Task.CompletedTask;
	}

	public Task AddFinalizedBranchAsync(Guid idOrchestrationInstance, IOrchestrationStep finalizedBranch, CancellationToken cancellationToken = default)
	{
		if (!_instances.TryGetValue(idOrchestrationInstance, out var orchestrationInstance))
			throw new InvalidOperationException($"No orchestration with {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} found");

		orchestrationInstance.AddFinalizedBranch(finalizedBranch);
		return Task.CompletedTask;
	}










	public Task<IResult<Guid>> SaveNewEventAsync(OrchestrationEvent @event, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<Guid>();

		if (@event == null)
			return Task.FromResult((IResult<Guid>)result.WithArgumentNullException(traceInfo, nameof(@event)));

		var instanceEventsDict = _eventsQueue.GetOrAdd(@event.OrchestrationKey, key => new ConcurrentDictionary<Guid, OrchestrationEvent>());

		instanceEventsDict.TryAdd(@event.Id, @event);
		return Task.FromResult((IResult<Guid>)result.Build());
	}

	public Task<IResult<List<OrchestrationEvent>?, Guid>> GetUnprocessedEventsAsync(string orchestrationKey, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<OrchestrationEvent>?, Guid>();

		if (string.IsNullOrWhiteSpace(orchestrationKey))
			return Task.FromResult(result.WithArgumentNullException(traceInfo, nameof(orchestrationKey)));

		if (_eventsQueue.TryGetValue(orchestrationKey, out var instanceEventsDict))
			result.WithData(instanceEventsDict.Values.Where(x => !x.ProcessedUtc.HasValue).ToList());

		return Task.FromResult(result.Build());
	}

	public Task<IResult<Guid>> UpdateEventAsync(OrchestrationEvent @event, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<Guid>();

		if (@event == null)
			return Task.FromResult((IResult<Guid>)result.WithArgumentNullException(traceInfo, nameof(@event)));

		if (_eventsQueue.TryGetValue(@event.OrchestrationKey, out var instanceEventsDict)
			&& instanceEventsDict.TryGetValue(@event.Id, out var existingEvent))
			instanceEventsDict.TryUpdate(@event.Id, @event, existingEvent);
		else
			return Task.FromResult((IResult<Guid>)result.WithInvalidOperationException(traceInfo, $"Nothing to update | {nameof(@event.OrchestrationKey)} = {@event.OrchestrationKey} | {nameof(@event.Id)} = {@event.Id}"));

		return Task.FromResult((IResult<Guid>)result.Build());
	}
}
