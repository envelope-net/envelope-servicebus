using Envelope.Extensions;
using Envelope.Infrastructure;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Orchestrations.Model.Internal;
using Envelope.Threading;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class OrchestrationExecutor : IOrchestrationExecutor
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IOrchestrationHostOptions _options;
	private readonly IOrchestrationRegistry _registry;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IOrchestrationLogger _logger;
	private readonly IDistributedLockProvider _lockProvider;
	private readonly IExecutionPointerFactory _pointerFactory;
	private readonly IOrchestrationRepository _orchestrationRepository;
	private readonly Lazy<IOrchestrationController> _orchestrationController;
	private readonly Lazy<IOrchestrationHost> _orchestrationHost;

	public IOrchestrationLogger OrchestrationLogger => _logger;
	public IOrchestrationHostOptions OrchestrationHostOptions => _options;


	public OrchestrationExecutor(
		IServiceProvider serviceProvider,
		IOrchestrationHostOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
		_lockProvider = options.DistributedLockProvider;
		_registry = options.OrchestrationRegistry;
		_logger = options.OrchestrationLogger(_serviceProvider);
		_pointerFactory = options.ExecutionPointerFactory(_serviceProvider);
		_orchestrationRepository = options.OrchestrationRepositoryFactory(_serviceProvider, _registry);
		_orchestrationController = new(() => _serviceProvider.GetRequiredService<IOrchestrationHost>().OrchestrationControllerInternal);
		_orchestrationHost = new(() => _serviceProvider.GetRequiredService<IOrchestrationHost>());
	}

	protected virtual ITransactionController CreateTransactionController()
		=> _serviceProvider.GetRequiredService<ITransactionCoordinator>().TransactionController;

	public async Task RestartAsync(
		IOrchestrationInstance orchestrationInstance,
		ITraceInfo traceInfo)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		if (orchestrationInstance.Status == OrchestrationStatus.Executing)
		{
			var localUpdateTransactionController = CreateTransactionController();

			await TransactionInterceptor.ExecuteAsync(
				false,
				traceInfo,
				localUpdateTransactionController,
				async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
				{
					await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Running, null, transactionController).ConfigureAwait(false);
					orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Running, null);
					transactionController.ScheduleCommit();
				},
				$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(OrchestrationStatus.Running)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
				async (traceInfo, exception, detail) =>
				{
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
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

		await orchestrationInstance.StartOrchestrationWorkerInternalAsync().ConfigureAwait(false);
	}

	public Task ExecuteAsync(
		IOrchestrationInstance orchestrationInstance,
		ITraceInfo traceInfo)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		_ = Task.Run(async () =>
		{
			try
			{
				await ExecuteInternalAsync(orchestrationInstance, traceInfo).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (orchestrationInstance.Status != OrchestrationStatus.Terminated)
				{
					var localTransactionController = CreateTransactionController();

					await TransactionInterceptor.ExecuteAsync(
						false,
						traceInfo,
						localTransactionController,
						async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
						{
							await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Terminated, null, transactionController).ConfigureAwait(false);
							orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Terminated, null);

							transactionController.ScheduleCommit();
						},
						$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteAsync)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}  - {nameof(_orchestrationRepository.UpdateOrchestrationStatusAsync)}",
						async (traceInfo, exception, detail) =>
						{
							await _logger.LogErrorAsync(
								traceInfo,
								orchestrationInstance?.IdOrchestrationInstance,
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

				var logMsg = ex.GetLogMessage();
				if (logMsg == null || !logMsg.IsLogged)
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						null,
						null,
						x => x.ExceptionInfo(ex).Detail($"{nameof(ExecuteAsync)} UNHANDLED EXCEPTION"),
						$"{nameof(ExecuteAsync)} UNHANDLED EXCEPTION",
						null,
						cancellationToken: default).ConfigureAwait(false);
			}
		});
		return Task.CompletedTask;
	}

	private readonly AsyncLock _executeLock = new();
	private async Task ExecuteInternalAsync(
		IOrchestrationInstance orchestrationInstance,
		ITraceInfo traceInfo)
	{
		if (orchestrationInstance.Status == OrchestrationStatus.Executing)
			return;

		List<ExecutionPointer>? exePointers = null;

		traceInfo = TraceInfo.Create(traceInfo);
		var startTransactionController = CreateTransactionController();

		var next = 
			await TransactionInterceptor.ExecuteAsync(
				false,
				traceInfo,
				startTransactionController,
				async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
				{
					exePointers = await GetExecutionPointersAsync(orchestrationInstance, traceInfo, transactionController).ConfigureAwait(false);

					if (exePointers.Count == 0
						&& (orchestrationInstance.Status == OrchestrationStatus.Running
							|| orchestrationInstance.Status == OrchestrationStatus.Executing))
					{
						await orchestrationInstance.StartOrchestrationWorkerInternalAsync().ConfigureAwait(false);
					}

					using (await _executeLock.LockAsync().ConfigureAwait(false))
					{
						if (orchestrationInstance.Status == OrchestrationStatus.Executing)
							return false;

						await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Executing, null, transactionController).ConfigureAwait(false);
						orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Executing, null);

						transactionController.ScheduleCommit();
					}

					return true;
				},
				$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(OrchestrationStatus.Executing)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
				async (traceInfo, exception, detail) =>
				{
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
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

		if (next != true)
			return;

		string lastLockOwner = string.Empty;
		bool locked = false;
		ExecutionPointer? currentPointer = null;
		try
		{
			while (0 < exePointers?.Count)
			{
				if (orchestrationInstance.Status != OrchestrationStatus.Running
					&& orchestrationInstance.Status != OrchestrationStatus.Executing)
					break; //GOTO:finally -> orchestrationInstance.StartOrchestrationWorkerAsync

				foreach (var pointer in exePointers)
				{
					var localExecuteTransactionController = CreateTransactionController();

					var loopControl =
						await TransactionInterceptor.ExecuteAsync(
							false,
							traceInfo,
							localExecuteTransactionController,
							async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
							{
								if (orchestrationInstance.Status != OrchestrationStatus.Running
									&& orchestrationInstance.Status != OrchestrationStatus.Executing)
									return LoopControlEnum.Break;

								if (!pointer.Active)
									return LoopControlEnum.Continue;

								var lockTimeout = DateTime.UtcNow.Add(pointer.GetStep().DistributedLockExpiration ?? orchestrationInstance.GetOrchestrationDefinition().DefaultDistributedLockExpiration);
								var lockOwner = _orchestrationHost.Value.HostInfo.HostName; //TODO lock owner - read owner from pointer.Step ?? maybe??
								lastLockOwner = lockOwner;

								var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default).ConfigureAwait(false);
								if (!lockResult.Succeeded)
									return LoopControlEnum.Return; //GOTO:finally -> orchestrationInstance.StartOrchestrationWorkerAsync

								locked = true;

								try
								{
									await InitializeStepAsync(orchestrationInstance, pointer, traceInfo, transactionController).ConfigureAwait(false);
									await ExecuteStepAsync(orchestrationInstance, pointer, traceInfo, transactionController, default).ConfigureAwait(false);
								}
								catch (Exception ex)
								{
									await RetryAsync(orchestrationInstance, pointer, null, traceInfo, transactionController, ex, "Unhandled exception").ConfigureAwait(false);
									return LoopControlEnum.Break;
								}

								transactionController.ScheduleCommit();

								return LoopControlEnum.None;
							},
							$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(ExecuteStepAsync)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
							async (traceInfo, exception, detail) =>
							{
								await _logger.LogErrorAsync(
									traceInfo,
									orchestrationInstance.IdOrchestrationInstance,
									currentPointer?.IdStep,
									currentPointer?.IdExecutionPointer,
									x => x.ExceptionInfo(exception).Detail(detail),
									detail,
									null,
									cancellationToken: default).ConfigureAwait(false);
							},
							null,
							true,
							cancellationToken: default).ConfigureAwait(false);

					switch (loopControl)
					{
						case LoopControlEnum.None:
							break;
						case LoopControlEnum.Continue:
							continue;
						case LoopControlEnum.Break:
							break;
						case LoopControlEnum.Return:
							return;  //GOTO:finally -> orchestrationInstance.StartOrchestrationWorkerAsync
						default:
							break;
					}

					if (loopControl == LoopControlEnum.Break)
						break;

					currentPointer = pointer;
				}

				var localGetTransactionController = CreateTransactionController();

				await TransactionInterceptor.ExecuteAsync(
					false,
					traceInfo,
					localGetTransactionController,
					async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
					{
						exePointers = await GetExecutionPointersAsync(orchestrationInstance, traceInfo, transactionController).ConfigureAwait(false);
						transactionController.ScheduleCommit();
					},
					$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(GetExecutionPointersAsync)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
					async (traceInfo, exception, detail) =>
					{
						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance?.IdOrchestrationInstance,
							currentPointer?.IdStep,
							currentPointer?.IdExecutionPointer,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);
					},
					null,
					true,
					cancellationToken: default).ConfigureAwait(false);
			}

			var localDetermineTransactionController = CreateTransactionController();

			await TransactionInterceptor.ExecuteAsync(
				false,
				traceInfo,
				localDetermineTransactionController,
				async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
				{
					await DetermineOrchestrationIsCompletedAsync(orchestrationInstance, traceInfo, transactionController).ConfigureAwait(false);
					transactionController.ScheduleCommit();
				},
				$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(DetermineOrchestrationIsCompletedAsync)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
				async (traceInfo, exception, detail) =>
				{
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance?.IdOrchestrationInstance,
						currentPointer?.IdStep,
						currentPointer?.IdExecutionPointer,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);
				},
				null,
				true,
				cancellationToken: default).ConfigureAwait(false);
		}
		finally
		{
			if (orchestrationInstance.Status == OrchestrationStatus.Executing)
			{
				var localUpdateTransactionController = CreateTransactionController();

				await TransactionInterceptor.ExecuteAsync(
					false,
					traceInfo,
					localUpdateTransactionController,
					async (traceInfo, transactionController, unhandledExceptionDetail, cancellationToken) =>
					{
						await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Running, null, transactionController).ConfigureAwait(false);
						orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Running, null);
						transactionController.ScheduleCommit();
					},
					$"{nameof(OrchestrationExecutor)} - {nameof(ExecuteInternalAsync)} {nameof(OrchestrationStatus.Running)} | {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}",
					async (traceInfo, exception, detail) =>
					{
						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance?.IdOrchestrationInstance,
							currentPointer?.IdStep,
							currentPointer?.IdExecutionPointer,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);
					},
					null,
					true,
					cancellationToken: default).ConfigureAwait(false);
			}

			if (locked)
			{
				try
				{
					var releaseResult = await _lockProvider.ReleaseLockAsync(orchestrationInstance, new SyncData(lastLockOwner)).ConfigureAwait(false); //TODO owner
					if (!releaseResult.Succeeded)
						; //TODO rollback transaction + log fatal error + throw fatal exception
				}
				catch (Exception ex)
				{
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						currentPointer?.IdStep,
						currentPointer?.IdExecutionPointer,
						x => x.ExceptionInfo(ex).Detail(nameof(_lockProvider.ReleaseLockAsync)),
						nameof(_lockProvider.ReleaseLockAsync),
						null,
						cancellationToken: default).ConfigureAwait(false);
				}
			}

			if (orchestrationInstance.Status == OrchestrationStatus.Running
				|| orchestrationInstance.Status == OrchestrationStatus.Executing)
				await orchestrationInstance.StartOrchestrationWorkerInternalAsync().ConfigureAwait(false);
		}
	}

	private async Task<List<ExecutionPointer>> GetExecutionPointersAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo, ITransactionController transactionController)
	{
		var nowUtc = DateTime.UtcNow;

		traceInfo = TraceInfo.Create(traceInfo);

		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return new List<ExecutionPointer>();

		var executionPointers =
			await _orchestrationRepository.GetOrchestrationExecutionPointersAsync(
				orchestrationInstance.IdOrchestrationInstance,
				transactionController).ConfigureAwait(false);

		var pointers = new List<ExecutionPointer>(
			executionPointers
				.Where(x => (x.Active && (!x.SleepUntilUtc.HasValue || (x.SleepUntilUtc < nowUtc && x.Status == PointerStatus.Retrying)))
					|| (!x.Active && x.SleepUntilUtc < nowUtc && x.Status == PointerStatus.Retrying)));

		var eventsResult = await _orchestrationRepository.GetUnprocessedEventsAsync(orchestrationInstance.OrchestrationKey, traceInfo, transactionController, default).ConfigureAwait(false);
		if (eventsResult.HasError)
			throw eventsResult.ToException()!;

		var eventNames = eventsResult.Data?.Select(x => x.EventName).ToList();
		if (eventNames != null)
		{
			var eventPointers = new List<ExecutionPointer>(
				executionPointers
					.Where(x => !string.IsNullOrWhiteSpace(x.EventName) && eventNames.Contains(x.EventName)));

			foreach (var eventPointer in eventPointers)
			{
				var @event = eventsResult.Data!.FirstOrDefault(x => x.EventName == eventPointer.EventName);
				if (@event == null)
					continue;

				//if event was used in previous iteration
				if (@event.ProcessedUtc.HasValue)
					continue;

				@event.ProcessedUtc = nowUtc;
				var updateEventResult = await _orchestrationRepository.SetProcessedUtcAsync(@event, traceInfo, transactionController, default).ConfigureAwait(false);
				if (updateEventResult.HasError)
					throw updateEventResult.ToException()!;

				var update = new ExecutionPointerUpdate(eventPointer.IdExecutionPointer)
				{
					Active = true,
					OrchestrationEvent = @event,
					Status = PointerStatus.InProcess
				};
				await _orchestrationRepository.UpdateExecutionPointerAsync(eventPointer, update, transactionController).ConfigureAwait(false);
				pointers.AddUniqueItem(eventPointer);
			}
		}

		var removedPointers = new List<ExecutionPointer>();
		var newPointers = new List<ExecutionPointer>();
		foreach (var pointer in pointers.Where(x => x.Status == PointerStatus.Retrying))
		{
			var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
			{
				Active = false,
				Status = PointerStatus.Completed
			};
			await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
			removedPointers.Add(pointer);

			var nextStep = pointer.GetStep().NextStep;
			if (nextStep != null)
			{
				var nextPointer = _pointerFactory.BuildNextPointer(
					orchestrationInstance,
					pointer,
					nextStep.IdStep);

				if (nextPointer == null)
					throw new InvalidOperationException($"{nameof(nextPointer)} == null | Step = {nextStep}");
				else
				{
					await _orchestrationRepository.AddExecutionPointerAsync(nextPointer, transactionController).ConfigureAwait(false);
				}

				newPointers.AddUniqueItem(nextPointer);
			}
		}

		foreach (var removedPointer in removedPointers)
			pointers.Remove(removedPointer);

		foreach (var newPointer in newPointers)
			pointers.AddUniqueItem(newPointer);

		return pointers;
	}

	private async Task InitializeStepAsync(IOrchestrationInstance orchestrationInstance, ExecutionPointer pointer, ITraceInfo traceInfo, ITransactionController transactionController)
	{
		if (pointer.Status != PointerStatus.InProcess)
		{
			var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
			{
				Status = PointerStatus.InProcess
			};

			if (!pointer.StartTimeUtc.HasValue)
				update.StartTimeUtc = DateTime.UtcNow;

			await _logger.LogTraceAsync(
				traceInfo,
				orchestrationInstance.IdOrchestrationInstance,
				pointer.IdStep,
				pointer.IdExecutionPointer,
				x => x.InternalMessage($"Step started {nameof(pointer.IdStep)} = {pointer.IdStep}"),
				null,
				null,
				cancellationToken: default).ConfigureAwait(false);

			await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
			await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(new StepStarted(orchestrationInstance, pointer), traceInfo, transactionController).ConfigureAwait(false);
		}
	}

	private async Task ExecuteStepAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		ITraceInfo traceInfo,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);

		IOrchestrationStep? finalizedBranch = null;
		if (pointer.PredecessorExecutionPointerStartingStepId.HasValue)
			finalizedBranch = pointer.GetStep().Branches.Values.FirstOrDefault(x => x.IdStep == pointer.PredecessorExecutionPointerStartingStepId.Value);

		if (finalizedBranch != null)
			await _orchestrationRepository.AddFinalizedBranchAsync(orchestrationInstance.IdOrchestrationInstance, finalizedBranch, transactionController, cancellationToken).ConfigureAwait(false);

		var finalizedBrancheIds = await _orchestrationRepository.GetFinalizedBrancheIdsAsync(orchestrationInstance.IdOrchestrationInstance, transactionController, cancellationToken).ConfigureAwait(false);

		var context = new StepExecutionContext(orchestrationInstance, pointer, finalizedBrancheIds, traceInfo)
		{
			CancellationToken = cancellationToken
		};

		var step = pointer.GetStep();
		using var scope = _serviceScopeFactory.CreateScope();
		await _logger.LogTraceAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			step.IdStep,
			pointer.IdExecutionPointer,
			x => x.InternalMessage($"Starting step {step.Name}"),
			null,
			null,
			cancellationToken).ConfigureAwait(false);

		var stepBody = step.ConstructBody(scope.ServiceProvider);
		step.SetInputParametersInternal?.Invoke(stepBody!, orchestrationInstance.Data, context);

		if (stepBody == null)
		{
			if (step is not EndOrchestrationStep)
				throw new NotSupportedException($"Invalid {nameof(step.BodyType)} type {step.BodyType?.GetType().FullName ?? "NULL"}");

			var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
			{
				Active = false,
				EndTimeUtc = DateTime.UtcNow,
				Status = PointerStatus.Completed
			};

			await _logger.LogErrorAsync(
				traceInfo,
				orchestrationInstance.IdOrchestrationInstance,
				null,
				null,
				x => x.InternalMessage($"Unable to construct step body {step.BodyType?.FullName ?? "NULL"}"),
				null,
				null,
				cancellationToken).ConfigureAwait(false);

			await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
			return;
		}

		IExecutionResult? result = null;
		if (step is not EndOrchestrationStep)
		{
			if (stepBody is ISyncStepBody syncStepBody)
			{
				result = syncStepBody.Run(context);
			}
			else if (stepBody is IAsyncStepBody asyncStepBody)
			{
				result = await asyncStepBody.RunAsync(context).ConfigureAwait(false);
			}
			else
			{
				throw new NotSupportedException($"Invalid {nameof(stepBody)} type {stepBody.GetType().FullName}");
			}
		}

		await ProcessExecutionResultAsync(orchestrationInstance, pointer, result, traceInfo, transactionController).ConfigureAwait(false);

		if (pointer.Status == PointerStatus.Completed && step.SetOutputParametersInternal != null)
			step.SetOutputParametersInternal(stepBody, orchestrationInstance.Data, context);
	}

	private async Task ProcessExecutionResultAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		IExecutionResult? result,
		ITraceInfo traceInfo,
		ITransactionController transactionController)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.GetStep();

		if (result != null)
		{
			if (result.Retry)
			{
				await RetryAsync(orchestrationInstance, pointer, result, traceInfo, transactionController, null, null).ConfigureAwait(false);
			}
			else if (!string.IsNullOrEmpty(result.EventName))
			{
				var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
				{
					EventName = result.EventName,
					EventKey = result.EventKey,
					Active = false,
					Status = PointerStatus.WaitingForEvent,
					EventWaitingTimeToLiveUtc = result.EventWaitingTimeToLiveUtc
				};

				var detail = $"{nameof(pointer.Status)} = {pointer.Status}";

				await _logger.LogTraceAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					pointer.IdStep,
					pointer.IdExecutionPointer,
					x => x.InternalMessage($"{nameof(result.EventName)} = {result.EventName} | {nameof(result.EventKey)} = {result.EventKey}").Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
				return;
			}
			else
			{
				var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
				{
					Active = false,
					EndTimeUtc = DateTime.UtcNow,
					Status = PointerStatus.Completed
				};

				var detail = $"{nameof(pointer.Status)} = {pointer.Status}";

				await _logger.LogTraceAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					pointer.IdStep,
					pointer.IdExecutionPointer,
					x => x.Detail(detail),
					detail,
					null,
					cancellationToken: default).ConfigureAwait(false);

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
				await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(new StepCompleted(orchestrationInstance, pointer), traceInfo, transactionController).ConfigureAwait(false);
			}

			if (result.NestedSteps != null)
			{
				foreach (var idNestedStep in result.NestedSteps.Distinct())
				{
					var nestedExecutionPointer = _pointerFactory.BuildNestedPointer(
						orchestrationInstance,
						pointer,
						idNestedStep);

					//pointer.AddNestedExecutionPointer(nestedExecutionPointer);
					//nestedExecutionPointer.ContainerExecutionPointer = pointer;

					if (nestedExecutionPointer != null)
					{
						await _orchestrationRepository.AddNestedExecutionPointerAsync(nestedExecutionPointer, pointer, transactionController).ConfigureAwait(false);
						//await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, transactionController).ConfigureAwait(false);
					}
				}
			}
			else if (result.NextSteps != null)
			{
				foreach (var idNextStep in result.NextSteps.Distinct())
				{
					if (idNextStep == Constants.Guids.FULL)
					{
						if (step.NextStep != null)
						{
							var executionPointer = _pointerFactory.BuildNextPointer(
								orchestrationInstance,
								pointer,
								step.NextStep.IdStep);

							if (executionPointer == null)
							{
								throw new InvalidOperationException($"{nameof(idNextStep)} == {nameof(Constants.Guids.FULL)} | {nameof(executionPointer)} == null");
							}
							else
							{
								await _orchestrationRepository.AddExecutionPointerAsync(executionPointer, transactionController).ConfigureAwait(false);
							}
						}
						else
						{
							var createdNextPointer = false;
							var branchController = step.BranchController;
							if (branchController != null)
							{
								var branchControllerExecutionPointer = 
									await _orchestrationRepository.GetStepExecutionPointerAsync(
										orchestrationInstance.IdOrchestrationInstance,
										branchController.IdStep,
										transactionController).ConfigureAwait(false);

								if (branchControllerExecutionPointer == null)
									throw new InvalidOperationException($"{nameof(branchControllerExecutionPointer)} == null | {nameof(step)} = {step}");

								var executionPointer = _pointerFactory.BuildNextPointer(
									orchestrationInstance,
									pointer,
									branchControllerExecutionPointer.IdStep);

								if (executionPointer == null)
								{
									throw new InvalidOperationException($"{nameof(branchController)} != null | {nameof(executionPointer)} == null");
								}
								else
								{
									await _orchestrationRepository.AddExecutionPointerAsync(executionPointer, transactionController).ConfigureAwait(false);
								}

								createdNextPointer = true;
								break; //while
							}

							if (!createdNextPointer)
							{
								if (step is not EndOrchestrationStep)
									throw new InvalidOperationException($"Step {step} has no {nameof(branchController)} and no next step");
							}
						}
					}
					else
					{
						var executionPointer = _pointerFactory.BuildNextPointer(
							orchestrationInstance,
							pointer,
							idNextStep);

						if (executionPointer == null)
						{
							throw new InvalidOperationException($"{nameof(idNextStep)} = {idNextStep} | {nameof(executionPointer)} == null");
						}
						else
						{
							await _orchestrationRepository.AddExecutionPointerAsync(executionPointer, transactionController).ConfigureAwait(false);
						}
					}
				}
			}
		}
		else //result == null
		{
			await SuspendAsync(orchestrationInstance, pointer, traceInfo, transactionController, null, $"{nameof(result)} == NULL").ConfigureAwait(false);
		}
	}

	private async Task DetermineOrchestrationIsCompletedAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo, ITransactionController transactionController)
	{
		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return;

		var executionPointers =
			await _orchestrationRepository.GetOrchestrationExecutionPointersAsync(
				orchestrationInstance.IdOrchestrationInstance,
				transactionController).ConfigureAwait(false);

		var hasCompletedEndStep = executionPointers.Any(x => x.GetStep() is EndOrchestrationStep && x.Status == PointerStatus.Completed);
		var hasNotEndedPointer = executionPointers.Any(x => x.EndTimeUtc == null);

		if (!hasCompletedEndStep && hasNotEndedPointer)
			return;

		var allPointersAreCompleted = executionPointers.Where(x => x.Status != PointerStatus.Completed).Select(x => x.ToString()).ToList();
		if (0 < allPointersAreCompleted.Count)
			throw new InvalidOperationException($"{nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance} | Not all {nameof(executionPointers)} were completed | {string.Join(Environment.NewLine, allPointersAreCompleted)}");

		await _logger.LogInformationAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			null,
			null,
			x => x.InternalMessage($"Orchestration completed {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}"),
			null,
			null,
			cancellationToken: default).ConfigureAwait(false);

		var utcNow = DateTime.UtcNow;
		await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Completed, utcNow, transactionController).ConfigureAwait(false);
		orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Completed, utcNow);
		await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(new OrchestrationCompleted(orchestrationInstance), traceInfo, transactionController).ConfigureAwait(false);
	}

	private async Task RetryAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		IExecutionResult? result,
		ITraceInfo traceInfo,
		ITransactionController transactionController,
		Exception? exception,
		string? detailMessage)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.GetStep();

		if (step.CanRetry(pointer.RetryCount))
		{
			var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
			{
				RetryCount = pointer.RetryCount + 1,
			};

			var retryInterval = result?.RetryInterval ?? step.GetRetryInterval(pointer.RetryCount);

			if (retryInterval.HasValue)
			{
				update.Active = true;
				update.SleepUntilUtc = DateTime.UtcNow.Add(retryInterval.Value);
				update.Status = PointerStatus.Retrying;

				var detail = string.IsNullOrWhiteSpace(detailMessage)
					? $"{nameof(pointer.Status)} = {pointer.Status}"
					: $"{detailMessage} | {nameof(pointer.Status)} = {pointer.Status}";

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);

				if (result == null || result.IsError)
				{
					var errorMessage =
						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance.IdOrchestrationInstance,
							pointer.IdStep,
							pointer.IdExecutionPointer,
							x => x.InternalMessage($"{nameof(pointer.RetryCount)} = {pointer.RetryCount}").Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

					await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(
						new OrchestrationError(orchestrationInstance, pointer, errorMessage), traceInfo, transactionController).ConfigureAwait(false);
				}
			}
			else
			{
				var detail = string.IsNullOrWhiteSpace(detailMessage)
					? $"{nameof(pointer.RetryCount)} = {pointer.RetryCount}"
					: $"{detailMessage} | {nameof(pointer.RetryCount)} = {pointer.RetryCount}";

				await SuspendAsync(orchestrationInstance, pointer, traceInfo, transactionController, exception, detail).ConfigureAwait(false);
			}
		}
		else
		{
			var detail = string.IsNullOrWhiteSpace(detailMessage)
				? $"{nameof(step.CanRetry)} = false"
				: $"{detailMessage} | {nameof(step.CanRetry)} = false";

			await SuspendAsync(orchestrationInstance, pointer, traceInfo, transactionController, exception, detail).ConfigureAwait(false);
		}
	}

	private async Task SuspendAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		ITraceInfo traceInfo,
		ITransactionController transactionController,
		Exception? exception,
		string? detailMessage)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.GetStep();
		var nowUtc = DateTime.UtcNow;

		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return;

		var update = new ExecutionPointerUpdate(pointer.IdExecutionPointer)
		{
			Active = false,
			EndTimeUtc = nowUtc,
			Status = PointerStatus.Suspended
		};

		var detail = $"Suspended orchestration: {nameof(pointer.Status)} = {pointer.Status}{(!string.IsNullOrWhiteSpace(detailMessage) ? $" | {detailMessage}" : "")}";

		await _logger.LogErrorAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			pointer.IdStep,
			pointer.IdExecutionPointer,
			x => x.ExceptionInfo(exception).Detail(detail),
			detail,
			null,
			cancellationToken: default).ConfigureAwait(false);

		await _orchestrationRepository.UpdateExecutionPointerAsync(pointer, update, transactionController).ConfigureAwait(false);
		var utcNow = DateTime.UtcNow;
		await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Suspended, utcNow, transactionController).ConfigureAwait(false);
		orchestrationInstance.UpdateOrchestrationStatus(OrchestrationStatus.Suspended, utcNow);

		await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(new StepSuspended(orchestrationInstance, pointer), traceInfo, transactionController).ConfigureAwait(false);
		await _orchestrationController.Value.PublishLifeCycleEventInternalAsync(new OrchestrationSuspended(orchestrationInstance, SuspendSource.ByExecutor), traceInfo, transactionController).ConfigureAwait(false);
	}
}
