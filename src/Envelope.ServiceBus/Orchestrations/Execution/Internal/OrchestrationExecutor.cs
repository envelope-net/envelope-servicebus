using Envelope.Extensions;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Orchestrations.Model.Internal;
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.Threading;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class OrchestrationExecutor : IOrchestrationExecutor
{
	private readonly IOrchestrationRegistry _registry;
	private readonly IServiceProvider _serviceProvider;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IOrchestrationLogger _logger;
	private readonly IDistributedLockProvider _lockProvider;
	private readonly IExecutionPointerFactory _pointerFactory;
	private readonly IOrchestrationRepository _orchestrationRepository;
	private readonly Lazy<IOrchestrationController> _orchestrationController;
	private readonly Lazy<IOrchestrationHost> _orchestrationHost;

	public OrchestrationExecutor(
		IOrchestrationRegistry registry,
		IServiceProvider serviceProvider,
		IServiceScopeFactory serviceScopeFactory,
		IDistributedLockProvider lockProvider,
		IExecutionPointerFactory pointerFactory,
		IOrchestrationRepository orchestrationRepository,
		IOrchestrationLogger logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
		_lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
		_registry = registry ?? throw new ArgumentNullException(nameof(registry));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_pointerFactory = pointerFactory ?? throw new ArgumentNullException(nameof(pointerFactory));
		_orchestrationRepository = orchestrationRepository ?? throw new ArgumentNullException(nameof(orchestrationRepository));
		_orchestrationController = new(() => _serviceProvider.GetRequiredService<IOrchestrationController>());
		_orchestrationHost = new(() => _serviceProvider.GetRequiredService<IOrchestrationHost>());
	}

	public Task ExecuteAsync(
		IOrchestrationInstance orchestrationInstance,
		ITraceInfo traceInfo)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				await ExecuteInternalAsync(orchestrationInstance, traceInfo);
			}
			catch (Exception ex)
			{
				if (orchestrationInstance.Status != OrchestrationStatus.Terminated)
				{
					try
					{
						await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Terminated);
					}
					catch (Exception updateEx)
					{
						orchestrationInstance.Status = OrchestrationStatus.Terminated;

						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance.IdOrchestrationInstance,
							null,
							null,
							x => x.ExceptionInfo(updateEx).Detail(nameof(_orchestrationRepository.UpdateOrchestrationStatusAsync)),
							nameof(_orchestrationRepository.UpdateOrchestrationStatusAsync),
							null,
							cancellationToken: default);
					}
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
						cancellationToken: default);
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

		traceInfo = TraceInfo.Create(traceInfo);

		var exePointers = await GetExecutionPointersAsync(orchestrationInstance, traceInfo);

		if (exePointers.Count == 0
			&& (orchestrationInstance.Status == OrchestrationStatus.Running
				|| orchestrationInstance.Status == OrchestrationStatus.Executing))
		{
			await orchestrationInstance.StartOrchestrationWorkerAsync();
		}

		using (await _executeLock.LockAsync().ConfigureAwait(false))
		{
			if (orchestrationInstance.Status == OrchestrationStatus.Executing)
				return;

			await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Executing);
		}

		string lastLockOwner = string.Empty;
		bool locked = false;
		ExecutionPointer? currentPointer = null;
		try
		{
			while (0 < exePointers.Count)
			{
				if (orchestrationInstance.Status != OrchestrationStatus.Running
					&& orchestrationInstance.Status != OrchestrationStatus.Executing)
					break; //GOTO:finally -> orchestrationInstance.StartOrchestrationWorkerAsync

				foreach (var pointer in exePointers)
				{
					currentPointer = pointer;

					if (orchestrationInstance.Status != OrchestrationStatus.Running
						&& orchestrationInstance.Status != OrchestrationStatus.Executing)
						break;

					if (!pointer.Active)
						continue;

					var lockTimeout = DateTime.UtcNow.Add(pointer.Step.DistributedLockExpiration ?? orchestrationInstance.OrchestrationDefinition.DefaultDistributedLockExpiration);
					var lockOwner = _orchestrationHost.Value.HostInfo.HostName; //TODO lock owner - read owner from pointer.Step ?? maybe??
					lastLockOwner = lockOwner;

					var lockResult = await _lockProvider.AcquireLockAsync(orchestrationInstance, lockOwner, lockTimeout, default);
					if (!lockResult.Succeeded)
						return; //GOTO:finally -> orchestrationInstance.StartOrchestrationWorkerAsync

					locked = true;

					try
					{
						await InitializeStepAsync(orchestrationInstance, pointer, traceInfo);
						await ExecuteStepAsync(orchestrationInstance, pointer, traceInfo, cancellationToken: default);
					}
					catch (Exception ex)
					{
						await RetryAsync(orchestrationInstance, pointer, null, traceInfo, ex, "Unhandled exception");
						break;
					}
				}

				exePointers = await GetExecutionPointersAsync(orchestrationInstance, traceInfo);
			}

			await DetermineOrchestrationIsCompletedAsync(orchestrationInstance, traceInfo);
		}
		finally
		{
			try
			{
				if (orchestrationInstance.Status == OrchestrationStatus.Executing)
					await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Running);
			}
			catch (Exception ex)
			{
				await _logger.LogErrorAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					currentPointer?.Step.IdStep,
					currentPointer?.IdExecutionPointer,
					x => x.ExceptionInfo(ex).Detail(nameof(_orchestrationRepository.UpdateOrchestrationStatusAsync)),
					nameof(_orchestrationRepository.UpdateOrchestrationStatusAsync),
					null,
					cancellationToken: default);
			}

			if (locked)
			{
				try
				{
					var releaseResult = await _lockProvider.ReleaseLockAsync(orchestrationInstance, new SyncData(lastLockOwner)); //TODO owner
					if (!releaseResult.Succeeded)
						; //TODO rollback transaction + log fatal error + throw fatal exception
				}
				catch (Exception ex)
				{
					await _logger.LogErrorAsync(
						traceInfo,
						orchestrationInstance.IdOrchestrationInstance,
						currentPointer?.Step.IdStep,
						currentPointer?.IdExecutionPointer,
						x => x.ExceptionInfo(ex).Detail(nameof(_lockProvider.ReleaseLockAsync)),
						nameof(_lockProvider.ReleaseLockAsync),
						null,
						cancellationToken: default);
				}
			}

			if (orchestrationInstance.Status == OrchestrationStatus.Running
				|| orchestrationInstance.Status == OrchestrationStatus.Executing)
				await orchestrationInstance.StartOrchestrationWorkerAsync();
		}
	}

	private async Task<List<ExecutionPointer>> GetExecutionPointersAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo)
	{
		var nowUtc = DateTime.UtcNow;

		traceInfo = TraceInfo.Create(traceInfo);

		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return new List<ExecutionPointer>();

		var pointers = new List<ExecutionPointer>(
			orchestrationInstance.ExecutionPointers
				.Where(x => (x.Active && (!x.SleepUntilUtc.HasValue || (x.SleepUntilUtc < nowUtc && x.Status == PointerStatus.Retrying)))
					|| (!x.Active && x.SleepUntilUtc < nowUtc && x.Status == PointerStatus.Retrying)));

		var eventsResult = await _orchestrationRepository.GetUnprocessedEventsAsync(orchestrationInstance.OrchestrationKey, traceInfo, default);
		if (eventsResult.HasError)
			throw eventsResult.ToException()!;

		var eventNames = eventsResult.Data?.Select(x => x.EventName).ToList();
		if (eventNames != null)
		{
			var eventPointers = new List<ExecutionPointer>(
				orchestrationInstance.ExecutionPointers
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
				var updateEventResult = await _orchestrationRepository.UpdateEventAsync(@event, traceInfo, default);
				if (updateEventResult.HasError)
					throw updateEventResult.ToException()!;

				eventPointer.Active = true;
				eventPointer.OrchestrationEvent = @event;
				eventPointer.Status = PointerStatus.InProcess;
				await _orchestrationRepository.UpdateExecutionPointerAsync(eventPointer);
				pointers.AddUniqueItem(eventPointer);
			}
		}

		var removedPointers = new List<ExecutionPointer>();
		var newPointers = new List<ExecutionPointer>();
		foreach (var pointer in pointers.Where(x => x.Status == PointerStatus.Retrying))
		{
			pointer.Active = false;
			pointer.Status = PointerStatus.Completed;
			await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);
			removedPointers.Add(pointer);

			if (pointer.Step.NextStep != null)
			{
				var nextPointer = _pointerFactory.BuildNextPointer(
					orchestrationInstance.OrchestrationDefinition,
					pointer,
					pointer.Step.NextStep.IdStep);

				if (nextPointer == null)
					throw new InvalidOperationException($"{nameof(nextPointer)} == null | Step = {pointer.Step.NextStep}");
				else
					await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, nextPointer);

				newPointers.AddUniqueItem(nextPointer);
			}
		}

		foreach (var removedPointer in removedPointers)
			pointers.Remove(removedPointer);

		foreach (var newPointer in newPointers)
			pointers.AddUniqueItem(newPointer);

		return pointers;
	}

	private async Task InitializeStepAsync(IOrchestrationInstance orchestrationInstance, ExecutionPointer pointer, ITraceInfo traceInfo)
	{
		if (pointer.Status != PointerStatus.InProcess)
		{
			pointer.Status = PointerStatus.InProcess;

			if (!pointer.StartTimeUtc.HasValue)
				pointer.StartTimeUtc = DateTime.UtcNow;

			await _logger.LogTraceAsync(
				traceInfo,
				orchestrationInstance.IdOrchestrationInstance,
				pointer.Step.IdStep,
				pointer.IdExecutionPointer,
				x => x.InternalMessage($"Step started {nameof(pointer.Step.IdStep)} = {pointer.Step.IdStep}"),
				null,
				null,
				cancellationToken: default);

			await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);
			await _orchestrationController.Value.PublishLifeCycleEventAsync(new StepStarted(orchestrationInstance, pointer), traceInfo);
		}
	}

	private async Task ExecuteStepAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);

		IOrchestrationStep? finalizedBranch = null;
		if (pointer.PredecessorExecutionPointer?.Step.StartingStep != null)
			finalizedBranch = pointer.Step.Branches.Values.FirstOrDefault(x => x.IdStep == pointer.PredecessorExecutionPointer.Step.StartingStep.IdStep);

		if (finalizedBranch != null)
		{
			await _orchestrationRepository.AddFinalizedBranchAsync(orchestrationInstance.IdOrchestrationInstance, finalizedBranch, cancellationToken);
		}

		var context = new StepExecutionContext(orchestrationInstance, pointer, traceInfo)
		{
			CancellationToken = cancellationToken
		};

		var step = pointer.Step;
		using var scope = _serviceScopeFactory.CreateScope();
		await _logger.LogTraceAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			step.IdStep,
			pointer.IdExecutionPointer,
			x => x.InternalMessage($"Starting step {step.Name}"),
			null,
			null,
			cancellationToken);

		var stepBody = step.ConstructBody(scope.ServiceProvider);
		step.SetInputParameters?.Invoke(stepBody!, orchestrationInstance.Data, context);

		if (stepBody == null)
		{
			if (step is not EndOrchestrationStep)
				throw new NotSupportedException($"Invalid {nameof(step.BodyType)} type {step.BodyType?.GetType().FullName ?? "NULL"}");

			pointer.Active = false;
			pointer.EndTimeUtc = DateTime.UtcNow;
			pointer.Status = PointerStatus.Completed;

			await _logger.LogErrorAsync(
				traceInfo,
				orchestrationInstance.IdOrchestrationInstance,
				null,
				null,
				x => x.InternalMessage($"Unable to construct step body {step.BodyType?.FullName ?? "NULL"}"),
				null,
				null,
				cancellationToken);

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
				result = await asyncStepBody.RunAsync(context);
			}
			else
			{
				throw new NotSupportedException($"Invalid {nameof(stepBody)} type {stepBody.GetType().FullName}");
			}
		}

		await ProcessExecutionResultAsync(orchestrationInstance, pointer, result, traceInfo);

		if (pointer.Status == PointerStatus.Completed && step.SetOutputParameters != null)
			step.SetOutputParameters(stepBody, orchestrationInstance.Data, context);
	}

	private async Task ProcessExecutionResultAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		IExecutionResult? result,
		ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.Step;

		if (result != null)
		{
			if (result.Retry)
			{
				await RetryAsync(orchestrationInstance, pointer, result, traceInfo, null, null);
			}
			else if (!string.IsNullOrEmpty(result.EventName))
			{
				pointer.EventName = result.EventName;
				pointer.EventKey = result.EventKey;
				pointer.Active = false;
				pointer.Status = PointerStatus.WaitingForEvent;
				pointer.EventWaitingTimeToLiveUtc = result.EventWaitingTimeToLiveUtc;
				var detail = $"{nameof(pointer.Status)} = {pointer.Status}";

				await _logger.LogTraceAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					pointer.Step.IdStep,
					pointer.IdExecutionPointer,
					x => x.InternalMessage($"{nameof(result.EventName)} = {result.EventName} | {nameof(result.EventKey)} = {result.EventKey}").Detail(detail),
					detail,
					null,
					cancellationToken: default);

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);
				return;
			}
			else
			{
				pointer.Active = false;
				pointer.EndTimeUtc = DateTime.UtcNow;
				pointer.Status = PointerStatus.Completed;
				var detail = $"{nameof(pointer.Status)} = {pointer.Status}";

				await _logger.LogTraceAsync(
					traceInfo,
					orchestrationInstance.IdOrchestrationInstance,
					pointer.Step.IdStep,
					pointer.IdExecutionPointer,
					x => x.Detail(detail),
					detail,
					null,
					cancellationToken: default);

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);
				await _orchestrationController.Value.PublishLifeCycleEventAsync(new StepCompleted(orchestrationInstance, pointer), traceInfo);
			}

			if (result.NestedSteps != null)
			{
				foreach (var idNestedStep in result.NestedSteps.Distinct())
				{
					var nestedExecutionPointer = _pointerFactory.BuildNestedPointer(
						orchestrationInstance.OrchestrationDefinition,
						pointer,
						idNestedStep);

					if (nestedExecutionPointer != null)
						await _orchestrationRepository.AddNestedExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, nestedExecutionPointer, pointer);
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
								orchestrationInstance.OrchestrationDefinition,
								pointer,
								step.NextStep.IdStep);

							if (executionPointer == null)
								; //TODO throw exception
							else
								await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, executionPointer);
						}
						else
						{
							var createdNextPointer = false;
							var branchController = step.BranchController;
							if (branchController != null)
							{
								var branchControllerExecutionPointer = await _orchestrationRepository.GetStepExecutionPointerAsync(
									orchestrationInstance.IdOrchestrationInstance,
									branchController.IdStep);

								if (branchControllerExecutionPointer == null)
									throw new InvalidOperationException($"{nameof(branchControllerExecutionPointer)} == null | {nameof(step)} = {step}");

								var executionPointer = _pointerFactory.BuildNextPointer(
									orchestrationInstance.OrchestrationDefinition,
									pointer,
									branchControllerExecutionPointer.Step.IdStep);

								if (executionPointer == null)
									; //TODO throw exception
								else
									await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, executionPointer);

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
							orchestrationInstance.OrchestrationDefinition,
							pointer,
							idNextStep);

						if (executionPointer == null)
							; //TODO throw exception
						else
							await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, executionPointer);
					}
				}
			}
		}
		else //result == null
		{
			await SuspendAsync(orchestrationInstance, pointer, traceInfo, null, $"{nameof(result)} == NULL");
		}
	}

	private async Task DetermineOrchestrationIsCompletedAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo)
	{
		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return;

		var hasCompletedEndStep = orchestrationInstance.ExecutionPointers.Any(x => x.Step is EndOrchestrationStep && x.Status == PointerStatus.Completed);

		if (!hasCompletedEndStep
			&& orchestrationInstance.ExecutionPointers.Any(x => x.EndTimeUtc == null))
			return;

		var allPointersAreCompleted = orchestrationInstance.ExecutionPointers.Where(x => x.Status != PointerStatus.Completed).Select(x => x.Step.ToString()).ToList();
		if (0 < allPointersAreCompleted.Count)
			throw new InvalidOperationException($"Not all {nameof(orchestrationInstance.ExecutionPointers)} were completed | {string.Join(Environment.NewLine, allPointersAreCompleted)}");

		await _logger.LogInformationAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			null,
			null,
			x => x.InternalMessage($"Orchestration completed {nameof(orchestrationInstance.IdOrchestrationInstance)} = {orchestrationInstance.IdOrchestrationInstance}"),
			null,
			null,
			cancellationToken: default);

		await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Completed, DateTime.UtcNow);
		await _orchestrationController.Value.PublishLifeCycleEventAsync(new OrchestrationCompleted(orchestrationInstance), traceInfo);
	}

	private async Task RetryAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		IExecutionResult? result,
		ITraceInfo traceInfo,
		Exception? exception,
		string? detailMessage)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.Step;

		if (step.CanRetry(pointer.RetryCount))
		{
			pointer.RetryCount++;

			var retryInterval = result?.RetryInterval ?? step.GetRetryInterval(pointer.RetryCount);

			if (retryInterval.HasValue)
			{
				pointer.Active = true;
				pointer.SleepUntilUtc = DateTime.UtcNow.Add(retryInterval.Value);
				pointer.Status = PointerStatus.Retrying;

				var detail = string.IsNullOrWhiteSpace(detailMessage)
					? $"{nameof(pointer.Status)} = {pointer.Status}"
					: $"{detailMessage} | {nameof(pointer.Status)} = {pointer.Status}";

				await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);

				if (result == null || result.IsError)
				{
					var errorMessage =
						await _logger.LogErrorAsync(
							traceInfo,
							orchestrationInstance.IdOrchestrationInstance,
							pointer.Step.IdStep,
							pointer.IdExecutionPointer,
							x => x.InternalMessage($"{nameof(pointer.RetryCount)} = {pointer.RetryCount}").Detail(detail),
							detail,
							null,
							cancellationToken: default);

					await _orchestrationController.Value.PublishLifeCycleEventAsync(
						new OrchestrationError(orchestrationInstance, pointer, errorMessage), traceInfo);
				}
			}
			else
			{
				var detail = string.IsNullOrWhiteSpace(detailMessage)
					? $"{nameof(pointer.RetryCount)} = {pointer.RetryCount}"
					: $"{detailMessage} | {nameof(pointer.RetryCount)} = {pointer.RetryCount}";

				await SuspendAsync(orchestrationInstance, pointer, traceInfo, exception, detail);
			}
		}
		else
		{
			var detail = string.IsNullOrWhiteSpace(detailMessage)
				? $"{nameof(step.CanRetry)} = false"
				: $"{detailMessage} | {nameof(step.CanRetry)} = false";

			await SuspendAsync(orchestrationInstance, pointer, traceInfo, exception, detail);
		}
	}

	private async Task SuspendAsync(
		IOrchestrationInstance orchestrationInstance,
		ExecutionPointer pointer,
		ITraceInfo traceInfo,
		Exception? exception,
		string? detailMessage)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var step = pointer.Step;
		var nowUtc = DateTime.UtcNow;

		if (orchestrationInstance.Status != OrchestrationStatus.Running
			&& orchestrationInstance.Status != OrchestrationStatus.Executing)
			return;

		pointer.Active = false;
		pointer.EndTimeUtc = nowUtc;
		pointer.Status = PointerStatus.Suspended;

		var detail = $"Suspended orchestration: {nameof(pointer.Status)} = {pointer.Status}{(!string.IsNullOrWhiteSpace(detailMessage) ? $" | {detailMessage}" : "")}";

		await _logger.LogErrorAsync(
			traceInfo,
			orchestrationInstance.IdOrchestrationInstance,
			pointer.Step.IdStep,
			pointer.IdExecutionPointer,
			x => x.ExceptionInfo(exception).Detail(detail),
			detail,
			null,
			cancellationToken: default);

		await _orchestrationRepository.UpdateExecutionPointerAsync(pointer);
		await _orchestrationRepository.UpdateOrchestrationStatusAsync(orchestrationInstance.IdOrchestrationInstance, OrchestrationStatus.Suspended, DateTime.UtcNow);

		await _orchestrationController.Value.PublishLifeCycleEventAsync(new StepSuspended(orchestrationInstance, pointer), traceInfo);
		await _orchestrationController.Value.PublishLifeCycleEventAsync(new OrchestrationSuspended(orchestrationInstance, SuspendSource.ByExecutor), traceInfo);
	}
}
