using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Internal;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Orchestrations.Model.Internal;
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class OrchestrationController : IOrchestrationController
{
	private readonly IOrchestrationRepository _orchestrationRepository;
	private readonly IOrchestrationExecutor _executor;
	private readonly IDistributedLockProvider _lockProvider;
	private readonly IOrchestrationRegistry _registry;
	private readonly IExecutionPointerFactory _pointerFactory;
	private readonly IServiceProvider _serviceProvider;
	private readonly IOrchestrationLogger _logger;
	private readonly Lazy<IOrchestrationHost> _orchestrationHost;

	public event OrchestrationEventHandler OnLifeCycleEvent;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public OrchestrationController(
		IOrchestrationRepository orchestrationRepository,
		IOrchestrationExecutor executor,
		IDistributedLockProvider lockProvider,
		IOrchestrationRegistry registry,
		IExecutionPointerFactory pointerFactory,
		IOrchestrationLogger logger,
		IServiceProvider serviceProvider)
	{
		_orchestrationRepository = orchestrationRepository ?? throw new ArgumentNullException(nameof(orchestrationRepository));
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
		_registry = registry ?? throw new ArgumentNullException(nameof(registry));
		_pointerFactory = pointerFactory ?? throw new ArgumentNullException(nameof(pointerFactory));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
		_orchestrationHost = new(() => _serviceProvider.GetRequiredService<IOrchestrationHost>());
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public void RegisterOrchestration<TOrchestration, TData>()
		where TOrchestration : IOrchestration<TData>
	{
		var orchestration = ActivatorUtilities.CreateInstance<TOrchestration>(_serviceProvider);
		_registry.RegisterOrchestration(orchestration);
	}

	public Task<IResult<Guid, Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		TData data,
		string lockOwner,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? workerIdleTimeout = null)
		=> StartOrchestrationAsync(idOrchestrationDefinition, orchestrationKey, null, data, lockOwner, traceInfo, workerIdleTimeout);

	public async Task<IResult<Guid, Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		int? version,
		TData data,
		string lockOwner,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? workerIdleTimeout = null)
	{
		var result = new ResultBuilder<Guid, Guid>();
		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		if (data == null)
			return result.WithArgumentNullException(traceInfo, nameof(data));

		var orchestrationDefinition = _registry.GetDefinition(idOrchestrationDefinition, version);
		if (orchestrationDefinition == null)
			return result.WithInvalidOperationException(traceInfo, $"Orchestration {idOrchestrationDefinition} {nameof(version)} = {version} is not registered");

		if (typeof(TData) != orchestrationDefinition.DataType)
			return result.WithInvalidOperationException(traceInfo, $"Orchestration {idOrchestrationDefinition} {nameof(version)} = {version} requires {nameof(orchestrationDefinition.DataType)} of type {orchestrationDefinition.DataType.GetType().FullName} | typeof(TData) = {typeof(TData).FullName}");

		var factory = () => 
			new OrchestrationInstance(
				orchestrationDefinition,
				orchestrationKey,
				data,
				_executor,
				_orchestrationHost.Value.HostInfo,
				workerIdleTimeout);

		var orchestrationInstance = orchestrationDefinition.GetOrSetSingletonInstance(factory, orchestrationKey);
		if (orchestrationInstance == null)
		{
			orchestrationInstance = factory();

			var genesisExecutionPointer = _pointerFactory.BuildGenesisPointer(orchestrationDefinition);
			if (genesisExecutionPointer != null)
				orchestrationInstance.AddExecutionPointer(genesisExecutionPointer);

			await _orchestrationRepository.CreateNewOrchestrationAsync(orchestrationInstance);

			await _logger.LogInformationAsync(
				traceInfo,
				orchestrationInstance.IdOrchestrationInstance,
				null,
				null,
				x => x.InternalMessage($"Started orchestration definition = {idOrchestrationDefinition} as instance = {orchestrationInstance.IdOrchestrationInstance}"),
				null,
				null,
				cancellationToken: default);

			await PublishLifeCycleEventAsync(new OrchestrationStarted(orchestrationInstance), traceInfo);
			await _executor.ExecuteAsync(orchestrationInstance, traceInfo);
		}

		return result.WithData(orchestrationInstance.IdOrchestrationInstance).Build();
	}

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance)
		=> _orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance);

	public async Task<IResult<bool, Guid>> SuspendOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo)
	{
		var result = new ResultBuilder<bool, Guid>();
		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		try
		{
			orchestrationInstance = await _orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance);
			if (orchestrationInstance == null)
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.InternalMessage($"No orchestration instance found with {nameof(idOrchestrationInstance)} == {idOrchestrationInstance}"),
					null,
					null,
					default);

				return result.WithData(false).Build();
			}

			var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.OrchestrationDefinition.DefaultDistributedLockExpiration);
			var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default);
			if (!lockResult.Succeeded)
				return result.WithData(false).Build();

			if (orchestrationInstance.Status == OrchestrationStatus.Running
				|| orchestrationInstance.Status == OrchestrationStatus.Executing)
			{
				await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Suspended, DateTime.UtcNow);

				await _logger.LogInformationAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					null,
					null,
					x => x.InternalMessage($"Suspended orchestration {nameof(idOrchestrationInstance)} = {idOrchestrationInstance}"),
					null,
					null,
					cancellationToken: default);

				await PublishLifeCycleEventAsync(new OrchestrationSuspended(orchestrationInstance, SuspendSource.ByController), traceInfo);

				return result.WithData(true).Build();
			}

			return result.WithData(false).Build();
		}
		finally
		{
			if (orchestrationInstance != null)
			{
				var releaseResult = await _lockProvider.ReleaseLockAsync(orchestrationInstance, new SyncData(lockOwner));
				if (!releaseResult.Succeeded)
					; //TODO rollback transaction + log fatal error + throw fatal exception
			}
		}
	}

	public async Task<IResult<bool, Guid>> ResumeOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo)
	{
		var result = new ResultBuilder<bool, Guid>();
		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		try
		{
			orchestrationInstance = await _orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance);
			if (orchestrationInstance == null)
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.InternalMessage($"No orchestration instance found with {nameof(idOrchestrationInstance)} == {idOrchestrationInstance}"),
					null,
					null,
					default);

				return result.WithData(false).Build();
			}

			var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.OrchestrationDefinition.DefaultDistributedLockExpiration);
			var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default);
			if (!lockResult.Succeeded)
				return result.WithData(false).Build();

			if (orchestrationInstance.Status == OrchestrationStatus.Suspended)
			{
				await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Running);

				await _logger.LogInformationAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					null,
					null,
					x => x.InternalMessage($"Resumed orchestration {nameof(idOrchestrationInstance)} = {idOrchestrationInstance}"),
					null,
					null,
					cancellationToken: default);

				await PublishLifeCycleEventAsync(new OrchestrationResumed(orchestrationInstance), traceInfo);

				return result.WithData(true).Build();
			}

			return result.WithData(false).Build();
		}
		finally
		{
			if (orchestrationInstance != null)
			{
				var releaseResult = await _lockProvider.ReleaseLockAsync(orchestrationInstance, new SyncData(lockOwner));
				if (!releaseResult.Succeeded)
					; //TODO rollback transaction + log fatal error + throw fatal exception
			}	
		}
	}

	public async Task<IResult<bool, Guid>> TerminateOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo)
	{
		var result = new ResultBuilder<bool, Guid>();
		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		try
		{
			orchestrationInstance = await _orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance);
			if (orchestrationInstance == null)
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.InternalMessage($"No orchestration instance found with {nameof(idOrchestrationInstance)} == {idOrchestrationInstance}"),
					null,
					null,
					default);

				return result.WithData(false).Build();
			}

			var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.OrchestrationDefinition.DefaultDistributedLockExpiration);
			var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default);
			if (!lockResult.Succeeded)
				return result.WithData(false).Build();

			if (orchestrationInstance.Status != OrchestrationStatus.Terminated)
			{
				await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Terminated, DateTime.UtcNow);

				await _logger.LogInformationAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					null,
					null,
					x => x.InternalMessage($"Terminated orchestration {nameof(idOrchestrationInstance)} =  {idOrchestrationInstance}"),
					null,
					null,
					cancellationToken: default);

				await PublishLifeCycleEventAsync(new OrchestrationTerminated(orchestrationInstance), traceInfo);

				return result.WithData(true).Build();
			}

			return result.WithData(false).Build();
		}
		finally
		{
			if (orchestrationInstance != null)
			{
				var releaseResult = await _lockProvider.ReleaseLockAsync(orchestrationInstance, new SyncData(lockOwner));
				if (!releaseResult.Succeeded)
					; //TODO rollback transaction + log fatal error + throw fatal exception
			}
		}
	}

	private async Task PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo<Guid> traceInfo)
	{
		if (OnLifeCycleEvent != null)
		{
			traceInfo = TraceInfo<Guid>.Create(traceInfo);

			try
			{
				//_ = Task.Run(async () => await OnLifeCycleEvent.Invoke(lifeCycleEvent));
				//return Task.CompletedTask;

				await OnLifeCycleEvent.Invoke(lifeCycleEvent, traceInfo);
			}
			catch (Exception ex)
			{
				IExecutionPointer? pointer = null;
				if (lifeCycleEvent is StepLifeCycleEvent stepLifeCycleEvent)
				{
					pointer = stepLifeCycleEvent.ExecutionPointer;
				}

				var detail = $"{nameof(PublishLifeCycleEventAsync)}: {nameof(lifeCycleEvent)} = {lifeCycleEvent.GetType().FullName}";

				await _logger.LogErrorAsync(
					traceInfo,
					lifeCycleEvent.OrchestrationInstance.IdOrchestrationInstance,
					pointer?.Step.IdStep,
					pointer?.IdExecutionPointer,
					x => x.ExceptionInfo(ex).Detail(detail),
					detail,
					null,
					default);
			}
		}
	}

	Task IOrchestrationController.PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo<Guid> traceInfo)
		=> PublishLifeCycleEventAsync(lifeCycleEvent, traceInfo);
}
