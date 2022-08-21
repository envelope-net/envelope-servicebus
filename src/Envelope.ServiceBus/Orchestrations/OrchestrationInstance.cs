using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.Timers;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations;

public class OrchestrationInstance : IOrchestrationInstance
{
	private readonly SequentialAsyncTimer _timer;

	private readonly IOrchestrationExecutor _executor;
	private readonly IServiceProvider _serviceProvider;
	private readonly IOrchestrationDefinition _orchestrationDefinition;

	private bool nextTimerStart;
	private bool _disposed;

	public Guid IdOrchestrationInstance { get; }

	public string OrchestrationKey { get; set; }

	public string DistributedLockResourceType => "Orchestration";

	public Guid IdOrchestrationDefinition { get; }

	public bool IsSingleton { get; }

	public bool AwaitForHandleLifeCycleEvents { get; }

	public OrchestrationStatus Status { get; set; }

	public int Version { get; }

	public object Data { get; }

	public DateTime CreateTimeUtc { get; internal set; }

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
		Guid idOrchestrationInstance,
		IOrchestrationDefinition orchestrationDefinition,
		string orchestrationKey,
		object data,
		IOrchestrationExecutor executor,
		IServiceProvider serviceProvider,
		TimeSpan? workerIdleTimeout)
	{
		IdOrchestrationInstance = idOrchestrationInstance;
		CreateTimeUtc = DateTime.UtcNow;
		Status = OrchestrationStatus.Running;

		_orchestrationDefinition = orchestrationDefinition ?? throw new ArgumentNullException(nameof(orchestrationDefinition));

		IdOrchestrationDefinition = _orchestrationDefinition.IdOrchestrationDefinition;
		IsSingleton = _orchestrationDefinition.IsSingleton;
		AwaitForHandleLifeCycleEvents = _orchestrationDefinition.AwaitForHandleLifeCycleEvents;
		Version = _orchestrationDefinition.Version;

		OrchestrationKey = !string.IsNullOrWhiteSpace(orchestrationKey)
			? orchestrationKey
			: throw new ArgumentNullException(nameof(orchestrationKey));
		Data = data ?? throw new ArgumentNullException(nameof(data));
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		nextTimerStart = false;
		_workerIdleTimeout = workerIdleTimeout ?? _orchestrationDefinition.WorkerIdleTimeout;
		_timer = new SequentialAsyncTimer(null, _workerIdleTimeout, _workerIdleTimeout, OnTimerAsync, OnTimerExceptionAsync);
	}

	public void UpdateOrchestrationStatus(OrchestrationStatus status, DateTime? completeTimeUtc)
	{
		Status = status;
		CompleteTimeUtc = completeTimeUtc;
	}

	public string CreateDistributedLockKey()
		=> $"{_orchestrationDefinition.IdOrchestrationDefinition}::{_orchestrationDefinition.Version}::{OrchestrationKey}";

	public Task<bool> StartOrchestrationWorkerAsync()
	{
		nextTimerStart = true;
		return _timer.StartAsync();
	}

	private async Task<bool> OnTimerAsync(object? state)
	{
		nextTimerStart = false;
		var traceInfo = TraceInfo.Create(_serviceProvider);
		await _executor.ExecuteAsync(this, traceInfo).ConfigureAwait(false);
		return nextTimerStart;
	}

	private async Task<bool> OnTimerExceptionAsync(object? state, Exception exception)
	{
		await _executor.OrchestrationLogger.LogErrorAsync(
			TraceInfo.Create(_serviceProvider),
			IdOrchestrationInstance,
			null,
			null,
			x => x.ExceptionInfo(exception).Detail(nameof(OnTimerExceptionAsync)),
			nameof(OnTimerExceptionAsync),
			null,
			cancellationToken: default).ConfigureAwait(false);

		return nextTimerStart;
	}

	public IOrchestrationExecutor GetExecutor()
		=> _executor;

	public IOrchestrationDefinition GetOrchestrationDefinition()
		=> _orchestrationDefinition;

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;

		_disposed = true;

		await DisposeAsyncCoreAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCoreAsync()
		=> _timer.DisposeAsync();

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		_disposed = true;

		if (disposing)
			_timer.Dispose();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
