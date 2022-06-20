using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Orchestrations.Model.Internal;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class OrchestrationController : IOrchestrationController
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IOrchestrationHostOptions _options;
	private readonly IDistributedLockProvider _lockProvider;
	private readonly IOrchestrationRegistry _registry;
	private readonly IExecutionPointerFactory _pointerFactory;
	private readonly IOrchestrationLogger _logger;
	private readonly Lazy<IOrchestrationHost> _orchestrationHost;

	public event OrchestrationEventHandler OnLifeCycleEvent;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public OrchestrationController(
		IServiceProvider serviceProvider,
		IOrchestrationHostOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_lockProvider = options.DistributedLockProvider;
		_registry = options.OrchestrationRegistry;
		_pointerFactory = options.ExecutionPointerFactory(_serviceProvider);
		_logger = options.OrchestrationLogger(_serviceProvider);
		_orchestrationHost = new(() => _serviceProvider.GetRequiredService<IOrchestrationHost>());
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public void RegisterOrchestration<TOrchestration, TData>()
		where TOrchestration : IOrchestration<TData>
	{
		var orchestration = ActivatorUtilities.CreateInstance<TOrchestration>(_serviceProvider);
		_registry.RegisterOrchestration(orchestration);
	}

	public Task<IResult<Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		TData data,
		string lockOwner,
		ITraceInfo traceInfo,
		TimeSpan? workerIdleTimeout = null)
		=> StartOrchestrationAsync(idOrchestrationDefinition, orchestrationKey, null, data, lockOwner, traceInfo, workerIdleTimeout);

	public async Task<IResult<Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		int? version,
		TData data,
		string lockOwner,
		ITraceInfo traceInfo,
		TimeSpan? workerIdleTimeout = null)
	{
		var result = new ResultBuilder<Guid>();
		traceInfo = TraceInfo.Create(traceInfo);

		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);

		IOrchestrationInstance? orchestrationInstance = null;

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionContext,
				async (traceInfo, transactionContext, cancellationToken) =>
				{
					if (data == null)
						return result.WithArgumentNullException(traceInfo, nameof(data));

					var orchestrationDefinition = _registry.GetDefinition(idOrchestrationDefinition, version);
					if (orchestrationDefinition == null)
						return result.WithInvalidOperationException(traceInfo, $"Orchestration {idOrchestrationDefinition} {nameof(version)} = {version} is not registered");

					if (typeof(TData) != orchestrationDefinition.DataType)
						return result.WithInvalidOperationException(traceInfo, $"Orchestration {idOrchestrationDefinition} {nameof(version)} = {version} requires {nameof(orchestrationDefinition.DataType)} of type {orchestrationDefinition.DataType.GetType().FullName} | typeof(TData) = {typeof(TData).FullName}");

					var idOrchestrationInstance = Guid.NewGuid();
					var factory = () =>
						new OrchestrationInstance(
							idOrchestrationInstance,
							orchestrationDefinition,
							orchestrationKey,
							data,
							OrchestrationExecutorManager.GetOrCreateOrchestrationExecutor(idOrchestrationInstance, _serviceProvider, _options),
							_orchestrationHost.Value.HostInfo,
							workerIdleTimeout);

					orchestrationInstance = orchestrationDefinition.GetOrSetSingletonInstance(factory, orchestrationKey);
					if (orchestrationInstance == null)
					{
						orchestrationInstance = factory();

						var orchestrationRepository = _options.OrchestrationRepositoryFactory(_serviceProvider, _registry);
						await orchestrationRepository.CreateNewOrchestrationAsync(orchestrationInstance, transactionContext, cancellationToken).ConfigureAwait(false);

						var genesisExecutionPointer = _pointerFactory.BuildGenesisPointer(orchestrationInstance);
						await orchestrationRepository.AddExecutionPointerAsync(
							genesisExecutionPointer,
							transactionContext).ConfigureAwait(false);

						await _logger.LogInformationAsync(
							traceInfo,
							orchestrationInstance.IdOrchestrationInstance,
							null,
							null,
							x => x.InternalMessage($"Started orchestration definition = {idOrchestrationDefinition} as instance = {orchestrationInstance.IdOrchestrationInstance}"),
							null,
							null,
							cancellationToken: default).ConfigureAwait(false);

						await PublishLifeCycleEventAsync(new OrchestrationStarted(orchestrationInstance), traceInfo, transactionContext).ConfigureAwait(false);

						transactionContext.ScheduleCommit();
					}

					return result.WithData(orchestrationInstance.IdOrchestrationInstance).Build();
				},
				$"{nameof(OrchestrationController)} - {nameof(StartOrchestrationAsync)}<{typeof(TData).FullName}> Global exception",
				async (traceInfo, exception, detail) =>
				{
					var errorMessage =
						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance?.IdOrchestrationInstance,
							null,
							null,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

					return errorMessage;
				},
				null,
				true,
				cancellationToken: default).ConfigureAwait(false);

		if (result.MergeHasError(executeResult))
			return result.Build();

		if (orchestrationInstance != null)
			await orchestrationInstance.GetExecutor().ExecuteAsync(orchestrationInstance, traceInfo).ConfigureAwait(false);

		return result.Build();
	}

	public async Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
	{
		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);
		var traceInfo = TraceInfo.Create(_options.HostName);

		return await TransactionInterceptor.ExecuteAsync(
			true,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var result = await _options.OrchestrationRepositoryFactory(_serviceProvider, _registry).GetOrchestrationInstanceAsync(idOrchestrationInstance, _serviceProvider, _options.HostInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				return result;
			},
			$"{nameof(OrchestrationController)} - {nameof(GetOrchestrationInstanceAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.ExceptionInfo(exception).Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	public async Task<bool?> IsCompletedOrchestrationAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
	{
		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);
		var traceInfo = TraceInfo.Create(_options.HostName);

		return await TransactionInterceptor.ExecuteAsync(
			true,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var result = await _options.OrchestrationRepositoryFactory(_serviceProvider, _registry).IsCompletedOrchestrationAsync(idOrchestrationInstance, transactionContext, cancellationToken).ConfigureAwait(false);

				//var inst = await _options.OrchestrationRepositoryFactory(_serviceProvider, _registry).GetOrchestrationInstanceAsync(orchestrationKey, _serviceProvider, _options.HostInfo, transactionContext, cancellationToken).ConfigureAwait(false);
				//var a = await _options.OrchestrationRepositoryFactory(_serviceProvider, _registry).GetOrchestrationExecutionPointersAsync(inst.IdOrchestrationInstance, transactionContext).ConfigureAwait(false);

				return result;
			},
			$"{nameof(OrchestrationController)} - {nameof(IsCompletedOrchestrationAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.ExceptionInfo(exception).Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	public async Task<List<ExecutionPointer>> GetOrchestrationExecutionPointersAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
	{
		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);
		var traceInfo = TraceInfo.Create(_options.HostName);

		return await TransactionInterceptor.ExecuteAsync(
			true,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var result = await _options.OrchestrationRepositoryFactory(_serviceProvider, _registry).GetOrchestrationExecutionPointersAsync(idOrchestrationInstance, transactionContext).ConfigureAwait(false);
				return result;
			},
			$"{nameof(OrchestrationController)} - {nameof(GetOrchestrationExecutionPointersAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				await _logger.LogErrorAsync(
					traceInfo,
					null,
					null,
					null,
					x => x.ExceptionInfo(exception).Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	public async Task<IResult<bool>> SuspendOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<bool>();
		traceInfo = TraceInfo.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var orchestrationRepository = _options.OrchestrationRepositoryFactory(_serviceProvider, _registry);
				orchestrationInstance = await orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance, _serviceProvider, _options.HostInfo, transactionContext, cancellationToken).ConfigureAwait(false);
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
						default).ConfigureAwait(false);

					return result.WithData(false).Build();
				}

				var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.GetOrchestrationDefinition().DefaultDistributedLockExpiration);
				var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default).ConfigureAwait(false);
				if (!lockResult.Succeeded)
					return result.WithData(false).Build();

				if (orchestrationInstance.Status == OrchestrationStatus.Running
					|| orchestrationInstance.Status == OrchestrationStatus.Executing)
				{
					var utcNow = DateTime.UtcNow;
					await orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Suspended, utcNow, transactionContext).ConfigureAwait(false);
					orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Suspended, utcNow);

					await _logger.LogInformationAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						null,
						null,
						x => x.InternalMessage($"Suspended orchestration {nameof(idOrchestrationInstance)} = {idOrchestrationInstance}"),
						null,
						null,
						cancellationToken: default).ConfigureAwait(false);

					await PublishLifeCycleEventAsync(new OrchestrationSuspended(orchestrationInstance, SuspendSource.ByController), traceInfo, transactionContext).ConfigureAwait(false);

					transactionContext.ScheduleCommit();

					return result.WithData(true).Build();
				}

				return result.WithData(false).Build();
			},
			$"{nameof(OrchestrationController)} - {nameof(SuspendOrchestrationAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
						null,
						null,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				return errorMessage;
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	public async Task<IResult<bool>> ResumeOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<bool>();
		traceInfo = TraceInfo.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var orchestrationRepository = _options.OrchestrationRepositoryFactory(_serviceProvider, _registry);
				orchestrationInstance = await orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance, _serviceProvider, _options.HostInfo, transactionContext, cancellationToken).ConfigureAwait(false);
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
						cancellationToken: default).ConfigureAwait(false);

					return result.WithData(false).Build();
				}

				var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.GetOrchestrationDefinition().DefaultDistributedLockExpiration);
				var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default).ConfigureAwait(false);
				if (!lockResult.Succeeded)
					return result.WithData(false).Build();

				if (orchestrationInstance.Status == OrchestrationStatus.Suspended)
				{
					await orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Running, null, transactionContext).ConfigureAwait(false);
					orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Running, null);

					await _logger.LogInformationAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						null,
						null,
						x => x.InternalMessage($"Resumed orchestration {nameof(idOrchestrationInstance)} = {idOrchestrationInstance}"),
						null,
						null,
						cancellationToken: default).ConfigureAwait(false);

					await PublishLifeCycleEventAsync(new OrchestrationResumed(orchestrationInstance), traceInfo, transactionContext).ConfigureAwait(false);

					transactionContext.ScheduleCommit();

					return result.WithData(true).Build();
				}

				return result.WithData(false).Build();
			},
			$"{nameof(OrchestrationController)} - {nameof(ResumeOrchestrationAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
						null,
						null,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				return errorMessage;
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	public async Task<IResult<bool>> TerminateOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<bool>();
		traceInfo = TraceInfo.Create(traceInfo);

		IOrchestrationInstance? orchestrationInstance = null;

		var transactionManager = _options.TransactionManagerFactory.Create();
		var transactionContext = await _options.TransactionContextFactory(_serviceProvider, transactionManager).ConfigureAwait(false);

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var orchestrationRepository = _options.OrchestrationRepositoryFactory(_serviceProvider, _registry);
				orchestrationInstance = await orchestrationRepository.GetOrchestrationInstanceAsync(idOrchestrationInstance, _serviceProvider, _options.HostInfo, transactionContext, cancellationToken).ConfigureAwait(false);
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
						cancellationToken: default).ConfigureAwait(false);

					return result.WithData(false).Build();
				}

				var lockTimeout = DateTime.UtcNow.Add(orchestrationInstance.GetOrchestrationDefinition().DefaultDistributedLockExpiration);
				var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default).ConfigureAwait(false);
				if (!lockResult.Succeeded)
					return result.WithData(false).Build();

				if (orchestrationInstance.Status != OrchestrationStatus.Terminated)
				{
					var utcNow = DateTime.UtcNow;
					await orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Terminated, utcNow, transactionContext).ConfigureAwait(false);
					orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Terminated, utcNow);

					await _logger.LogInformationAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						null,
						null,
						x => x.InternalMessage($"Terminated orchestration {nameof(idOrchestrationInstance)} =  {idOrchestrationInstance}"),
						null,
						null,
						cancellationToken: default).ConfigureAwait(false);

					await PublishLifeCycleEventAsync(new OrchestrationTerminated(orchestrationInstance), traceInfo, transactionContext).ConfigureAwait(false);

					transactionContext.ScheduleCommit();

					return result.WithData(true).Build();
				}

				return result.WithData(false).Build();
			},
			$"{nameof(OrchestrationController)} - {nameof(TerminateOrchestrationAsync)} {nameof(idOrchestrationInstance)} = {idOrchestrationInstance} Global exception",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
						null,
						null,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				return errorMessage;
			},
			null,
			true,
			cancellationToken: default).ConfigureAwait(false);
	}

	private async Task PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo traceInfo, ITransactionContext transactionContext)
	{
		if (OnLifeCycleEvent != null)
		{
			traceInfo = TraceInfo.Create(traceInfo);

			try
			{
				//_ = Task.Run(async () => await OnLifeCycleEvent.Invoke(lifeCycleEvent)).ConfigureAwait(false);
				//return Task.CompletedTask;

				await OnLifeCycleEvent.Invoke(lifeCycleEvent, traceInfo, transactionContext).ConfigureAwait(false);
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
					pointer?.IdStep,
					pointer?.IdExecutionPointer,
					x => x.ExceptionInfo(ex).Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);
			}
		}
	}

	Task IOrchestrationController.PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo traceInfo, ITransactionContext transactionContext)
		=> PublishLifeCycleEventAsync(lifeCycleEvent, traceInfo, transactionContext);
}
