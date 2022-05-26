using Envelope.Extensions;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.Timers;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Internal;

internal class OrchestrationInstance : IOrchestrationInstance
{
	private readonly SequentialAsyncTimer _timer;
	private bool nextTimerStart;
	private bool disposed;

	private readonly IOrchestrationExecutor _executor;
	private readonly IHostInfo _hostInfo;

	public Guid IdOrchestrationInstance { get; }

	public string OrchestrationKey { get; set; }

	public string DistributedLockResourceType => "Orchestration";

	public IOrchestrationDefinition OrchestrationDefinition { get; }

	public bool IsSingleton => OrchestrationDefinition.IsSingleton;

	public bool AwaitForHandleLifeCycleEvents => OrchestrationDefinition.AwaitForHandleLifeCycleEvents;

	public ExecutionPointerCollection ExecutionPointers { get; set; }

	IReadOnlyExecutionPointerCollection IOrchestrationInstance.ExecutionPointers => ExecutionPointers;

	public List<IOrchestrationStep> FinalizedBranches { get; }
	IReadOnlyList<IOrchestrationStep> IOrchestrationInstance.FinalizedBranches => FinalizedBranches;

	public OrchestrationStatus Status { get; set; }

	public int Version => OrchestrationDefinition.Version;

	public object Data { get; }

	public DateTime CreateTimeUtc { get; }

	public DateTime? CompleteTimeUtc { get; set; }

	private TimeSpan _workerIdleTimeout;
	public TimeSpan WorkerIdleTimeout
	{
		get
		{
			return _workerIdleTimeout;
		}
		set
		{
			_workerIdleTimeout = value;
			_timer.StartDelay = _workerIdleTimeout;
			_timer.TimerInterval = _workerIdleTimeout;
		}
	}

	public OrchestrationInstance(
		IOrchestrationDefinition orchestrationDefinition,
		string orchestrationKey,
		object data,
		IOrchestrationExecutor executor,
		IHostInfo hostInfo,
		TimeSpan? workerIdleTimeout)
	{
		IdOrchestrationInstance = Guid.NewGuid();
		CreateTimeUtc = DateTime.UtcNow;
		Status = OrchestrationStatus.Running;
		OrchestrationDefinition = orchestrationDefinition ?? throw new ArgumentNullException(nameof(orchestrationDefinition));
		OrchestrationKey = !string.IsNullOrWhiteSpace(orchestrationKey)
			? orchestrationKey
			: throw new ArgumentNullException(nameof(orchestrationKey));
		ExecutionPointers = new ExecutionPointerCollection();
		Data = data ?? throw new ArgumentNullException(nameof(data));
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_hostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
		nextTimerStart = false;
		_workerIdleTimeout = workerIdleTimeout ?? OrchestrationDefinition.WorkerIdleTimeout;
		FinalizedBranches = new();
		_timer = new SequentialAsyncTimer(null, _workerIdleTimeout, _workerIdleTimeout, OnTimerAsync, OnTimerExceptionAsync);
	}

	public void AddExecutionPointer(ExecutionPointer executionPointer)
	{
		if (executionPointer == null)
			throw new ArgumentNullException(nameof(executionPointer));

		ExecutionPointers.Add(executionPointer);
	}

	public void AddFinalizedBranch(IOrchestrationStep step)
		=> FinalizedBranches.AddUniqueItem(step);

	public ExecutionPointer? GetStepExecutionPointer(Guid idStep)
		=> ExecutionPointers.Where(x => x.Step.IdStep == idStep).FirstOrDefault();

	public void UpdateOrchestrationStatus(OrchestrationStatus status, DateTime? completeTimeUtc)
	{
		Status = status;
		CompleteTimeUtc = completeTimeUtc;
	}

	public string CreateDistributedLockKey()
		=> $"{OrchestrationDefinition.IdOrchestrationDefinition}::{OrchestrationDefinition.Version}::{OrchestrationKey}";

	public Task<bool> StartOrchestrationWorkerAsync()
	{
		nextTimerStart = true;
		return _timer.StartAsync();
	}

	private async Task<bool> OnTimerAsync(object? state)
	{
		nextTimerStart = false;
		var traceInfo = TraceInfo<Guid>.Create(_hostInfo.HostName);
		await _executor.ExecuteAsync(this, traceInfo);
		return nextTimerStart;
	}

	private Task<bool> OnTimerExceptionAsync(object? state, Exception exception)
	{
		//TODO log exception
		return Task.FromResult(nextTimerStart);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCoreAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCoreAsync()
		=> _timer.DisposeAsync();

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
				_timer.Dispose();

			disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
