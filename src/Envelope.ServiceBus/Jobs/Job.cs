using Envelope.Calendar;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Timers;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Jobs;

public abstract class Job : IJob
{
	public class SequentialTimerState
	{
		public Guid ExecutionId { get; }
		public DateTime StartedUtc { get; }

		public SequentialTimerState()
		{
			ExecutionId = Guid.NewGuid();
			StartedUtc = DateTime.UtcNow;
		}
	}

	private SequentialAsyncTimer? _sequentialAsyncTimer;
	private Timer? _periodicTimer;
	private CronAsyncTimer? _cronAsyncTimer;

	public Guid JobInstanceId { get; private set; }

	internal virtual Func<ITraceInfo, Task>? BeforeStartAsync { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected internal IServiceProvider MainServiceProvider { get; private set; }

	public IHostInfo HostInfo { get; private set; }

	internal IJobRepository JobRepository { get; private set; }

	protected IJobLogger Logger { get; private set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private bool _cronAsyncTimerIsStopped = false;

	protected abstract IJobConfiguration Config { get; }

	public bool Initialized { get; private set; }

	public string Name => Config.Name;

	public string? Description => Config.Description;

	public bool Disabled => Config.Disabled;

	public JobExecutingMode Mode => Config.Mode;

	public TimeSpan? DelayedStart { get; protected set; }

	public TimeSpan? IdleTimeout { get; protected set; }

	public CronTimerSettings? CronTimerSettings { get; private set; }

	public DateTime? NextExecutionRunUtc { get; private set; }

	public int ExecutionEstimatedTimeInSeconds { get; protected set; }

	public int DeclaringAsOfflineAfterMinutesOfInactivity { get; protected set; }

	public JobStatus Status { get; private set; }

	public DateTime LastUpdateUtc { get; private set; }

	public DateTime? LastExecutionStartedUtc { get; private set; }

	private readonly object _initLock = new();
	internal void Initialize(IJobProviderConfiguration config, IServiceProvider serviceProvider)
	{
		if (Initialized)
			throw new InvalidOperationException("Already initialized");

		if (config == null)
			throw new ArgumentNullException(nameof(config));

		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		lock (_initLock)
		{
			if (Initialized)
				throw new InvalidOperationException("Already initialized");

			if (Config == null)
				throw new InvalidOperationException($"{nameof(Config)} == null");

			JobInstanceId = Guid.NewGuid();

			MainServiceProvider = serviceProvider;
			HostInfo = config.HostInfoInternal ?? throw new InvalidOperationException($"{nameof(HostInfo)} == null");
			Logger = config.JobLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(Logger)} == null");
			JobRepository = config.JobRepository(serviceProvider) ?? throw new InvalidOperationException($"{nameof(JobRepository)} == null"); ;

			DelayedStart = Config.DelayedStart;
			IdleTimeout = Config.IdleTimeout;
			CronTimerSettings = Config.CronTimerSettings;

			ExecutionEstimatedTimeInSeconds = Config.ExecutionEstimatedTimeInSeconds;
			DeclaringAsOfflineAfterMinutesOfInactivity = Config.DeclaringAsOfflineAfterMinutesOfInactivity;

			var executeResult = new JobExecuteResult(JobInstanceId, true, JobExecuteStatus.NONE);

			_cronAsyncTimerIsStopped = false;
			SetStatus(Config.Disabled ? JobStatus.Disabled : JobStatus.Stopped);
			using var disableScopedServiceProvider = MainServiceProvider.CreateScope();
			Logger.LogStatus(TraceInfo.Create(disableScopedServiceProvider.ServiceProvider), this, executeResult, null);

			if (Status == JobStatus.Disabled)
				return;

			if (Mode == JobExecutingMode.SequentialIntervalTimer)
			{
				if (!IdleTimeout.HasValue)
					throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

				if (DelayedStart.HasValue)
				{
					_sequentialAsyncTimer = new SequentialAsyncTimer(null, () => new SequentialTimerState(), DelayedStart.Value, IdleTimeout.Value, OnSequentialAsyncTimerTickAsync, OnSequentialAsyncTimerExceptionAsync);
				}
				else
				{
					_sequentialAsyncTimer = new SequentialAsyncTimer(null, () => new SequentialTimerState(), IdleTimeout.Value, OnSequentialAsyncTimerTickAsync, OnSequentialAsyncTimerExceptionAsync);
				}
			}
			else if (Mode == JobExecutingMode.ExactPeriodicTimer)
			{
				if (!IdleTimeout.HasValue)
					throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

				if (DelayedStart.HasValue)
				{
					_periodicTimer = new Timer(ExactTimerCallbackAsync, null, Timeout.Infinite, Timeout.Infinite);
				}
				else
				{
					_periodicTimer = new Timer(ExactTimerCallbackAsync, null, Timeout.Infinite, Timeout.Infinite);
				}
			}
			else if (Mode == JobExecutingMode.Cron)
			{
				if (CronTimerSettings == null)
					throw new InvalidOperationException($"{nameof(CronTimerSettings)} == null");

				_cronAsyncTimer = new CronAsyncTimer(CronTimerSettings.CronExpression);
			}

			OnInitialize();

			Initialized = true;

			using var scopedServiceProvider = MainServiceProvider.CreateScope();
			Logger.LogStatus(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null);
		}
	}

	protected virtual void OnInitialize()
	{
	}

	private void SetStatus(JobStatus status)
	{
		Status = status;
		LastUpdateUtc= DateTime.UtcNow;
	}

	void IJob.InitializeInternal(IJobProviderConfiguration config, IServiceProvider serviceProvider)
		=> Initialize(config, serviceProvider);

	private Task<bool> OnSequentialAsyncTimerTickAsync(object? state)
	{
		NextExecutionRunUtc = DateTime.UtcNow.AddSeconds(ExecutionEstimatedTimeInSeconds).Add(IdleTimeout!.Value);

		return ExecuteInternalAsync();
	}

	private async Task<bool> OnSequentialAsyncTimerExceptionAsync(object? state, Exception exception)
	{
		if (state is not SequentialTimerState sequentialTimerState)
			sequentialTimerState = new SequentialTimerState();

		var executeResult = new JobExecuteResult(sequentialTimerState.ExecutionId, true, JobExecuteStatus.Failed);
		await OnUnhandledExceptionAsync(executeResult, TraceInfo.Create(MainServiceProvider), exception).ConfigureAwait(false);
		if (executeResult.ExecuteStatus == JobExecuteStatus.Running)
			executeResult.SetStatus(JobExecuteStatus.Invalid, true);

		await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
		if (executeResult.Continue)
		{
			SetStatus(JobStatus.Idle);
			await Logger.LogStatusAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null, cancellationToken: default).ConfigureAwait(false);
		}
		else
		{
			SetStatus(JobStatus.Stopped);
			await StopAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider)).ConfigureAwait(false);
		}

		await Logger.LogExecutionFinishedAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, sequentialTimerState.StartedUtc).ConfigureAwait(false);

		return executeResult.Continue;
	}

#pragma warning disable VSTHRD100 // Avoid async void methods
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
	private async void ExactTimerCallbackAsync(object? state)
	{
		NextExecutionRunUtc = DateTime.UtcNow.Add(IdleTimeout!.Value);

		await ExecuteInternalAsync();
	}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore VSTHRD100 // Avoid async void methods

	private Task OnNextCronTimerTickAsync()
	{
		if (_cronAsyncTimerIsStopped)
			return Task.CompletedTask;

		NextExecutionRunUtc = _cronAsyncTimer!.GetNextOccurrence(DateTimeOffset.Now)?.ToUniversalTime().DateTime;

		return ExecuteInternalAsync();
	}

	public async Task StartAsync(ITraceInfo traceInfo)
	{
		var executeResult = new JobExecuteResult(JobInstanceId, true, JobExecuteStatus.NONE);
		await Logger.LogInformationAsync(traceInfo, this, executeResult, null, LogCodes.STARTING, x => x.Detail("Starting"), "Starting", true, null, cancellationToken: default);

		if (!Initialized)
			throw new InvalidOperationException("Not initialized");

		if (Status == JobStatus.Disabled)
			throw new InvalidOperationException($"Cannot start disabled job {Name}");

		if (Status != JobStatus.Stopped)
			return;

		if (BeforeStartAsync != null)
			await BeforeStartAsync.Invoke(traceInfo).ConfigureAwait(false);

		SetStatus(JobStatus.Idle);
		await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
		await Logger.LogStatusAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null, cancellationToken: default).ConfigureAwait(false);

		var utcNow = DateTime.UtcNow;

		if (Mode == JobExecutingMode.SequentialIntervalTimer)
		{
			NextExecutionRunUtc = DelayedStart.HasValue
				? utcNow.Add(DelayedStart.Value)
				: utcNow;

			await _sequentialAsyncTimer!.StartAsync().ConfigureAwait(false);
		}
		else if (Mode == JobExecutingMode.ExactPeriodicTimer)
		{
			if (!IdleTimeout.HasValue)
				throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

			if (DelayedStart.HasValue)
			{
				NextExecutionRunUtc = utcNow.Add(DelayedStart.Value);
				_periodicTimer!.Change(DelayedStart.Value, IdleTimeout.Value);
			}
			else
			{
				NextExecutionRunUtc = utcNow;
				_periodicTimer!.Change(TimeSpan.Zero, IdleTimeout.Value);
			}
		}
		else if (Mode == JobExecutingMode.Cron)
		{
			_cronAsyncTimerIsStopped = false;
			NextExecutionRunUtc = _cronAsyncTimer!.GetNextOccurrence(DateTimeOffset.Now)?.ToUniversalTime().DateTime;

			_ = Task.Run(async () =>
			{
				while (await _cronAsyncTimer!.WaitForNextTickAsync().ConfigureAwait(false))
				{
					await OnNextCronTimerTickAsync().ConfigureAwait(false);

					if (_cronAsyncTimerIsStopped)
						return;
				}
			},
			cancellationToken: default);
		}

		await Logger.LogInformationAsync(traceInfo, this, executeResult, null, LogCodes.STARTED, x => x.Detail("Started"), "Started", true, null, cancellationToken: default);
	}

	public async Task StopAsync(ITraceInfo traceInfo)
	{
		var executeResult = new JobExecuteResult(JobInstanceId, true, JobExecuteStatus.NONE);
		await Logger.LogInformationAsync(traceInfo, this, executeResult, null, LogCodes.STOPPING, x => x.Detail("Stopping"), "Stopping", true, null, cancellationToken: default);

		if (!Initialized)
			throw new InvalidOperationException("Not initialized");

		if (Status == JobStatus.Disabled)
			throw new InvalidOperationException($"Cannot stop disabled job {Name}");

		await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();

		SetStatus(JobStatus.Stopped);
		await Logger.LogStatusAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null, cancellationToken: default).ConfigureAwait(false);

		if (Mode == JobExecutingMode.SequentialIntervalTimer)
		{
			await _sequentialAsyncTimer!.StopAsync().ConfigureAwait(false);
		}
		else if (Mode == JobExecutingMode.ExactPeriodicTimer)
		{
			_periodicTimer!.Change(Timeout.Infinite, Timeout.Infinite);
		}
		else if (Mode == JobExecutingMode.Cron)
		{
			_cronAsyncTimerIsStopped = true;
		}

		NextExecutionRunUtc = null;

		await Logger.LogInformationAsync(traceInfo, this, executeResult, null, LogCodes.STOPPED, x => x.Detail("Stopped"), "Stopped", true, null, cancellationToken: default);
	}

	public abstract Task ExecuteAsync(JobExecuteResult executeResult, IServiceProvider scopedServiceProvider);

	private async Task<bool> ExecuteInternalAsync()
	{
		SetStatus(JobStatus.InProcess);
		var executeResult = new JobExecuteResult(true);
		await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
		await Logger.LogStatusAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null, cancellationToken: default).ConfigureAwait(false);
		var utcNow = DateTime.UtcNow;
		await Logger.LogExecutionStartAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, utcNow, false).ConfigureAwait(false);
		LastExecutionStartedUtc = utcNow;

		try
		{
			await ExecuteAsync(executeResult, scopedServiceProvider.ServiceProvider).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			await OnUnhandledExceptionAsync(executeResult, TraceInfo.Create(MainServiceProvider), ex).ConfigureAwait(false);
		}

		if (executeResult.ExecuteStatus == JobExecuteStatus.Running)
			executeResult.SetStatus(JobExecuteStatus.Invalid, true);

		if (executeResult.Continue)
		{
			SetStatus(JobStatus.Idle);
			await Logger.LogStatusAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, null, cancellationToken: default).ConfigureAwait(false);
		}
		else
		{
			SetStatus(JobStatus.Stopped);
			await StopAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider)).ConfigureAwait(false);
		}

		await Logger.LogExecutionFinishedAsync(TraceInfo.Create(scopedServiceProvider.ServiceProvider), this, executeResult, utcNow).ConfigureAwait(false);

		if (Mode == JobExecutingMode.SequentialIntervalTimer)
		{
			NextExecutionRunUtc = DateTime.UtcNow.AddSeconds(ExecutionEstimatedTimeInSeconds).Add(IdleTimeout!.Value);
		}
		else if (Mode == JobExecutingMode.ExactPeriodicTimer)
		{
			NextExecutionRunUtc = DateTime.UtcNow.Add(IdleTimeout!.Value);
		}
		else if (Mode == JobExecutingMode.Cron)
		{
			NextExecutionRunUtc = _cronAsyncTimer!.GetNextOccurrence(DateTimeOffset.Now)?.ToUniversalTime().DateTime;
		}

		return executeResult.Continue;
	}

	public virtual async Task OnUnhandledExceptionAsync(JobExecuteResult executeResult, ITraceInfo traceInfo, Exception exception)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var detail = $"{this.GetType().FullName} >> {nameof(OnUnhandledExceptionAsync)}";
		await Logger.LogErrorAsync(traceInfo, this, executeResult, JobExecuteStatus.Failed, LogCodes.GLOBAL_ERROR, x => x.ExceptionInfo(exception).Detail(detail), detail, null, cancellationToken: default).ConfigureAwait(false);
	}

	public virtual T? GetData<T>()
		=> default;
}

public abstract class Job<TData> : Job, IJob<TData>, IJob
{
	public TData? Data { get; private set; }

	internal override Func<ITraceInfo, Task>? BeforeStartAsync => BeforeStartInternalAsync;

	internal ITransactionController CreateTransactionController()
		=> MainServiceProvider.GetRequiredService<ITransactionCoordinator>().TransactionController;

	private async Task BeforeStartInternalAsync(ITraceInfo traceInfo)
	{
		var result = new ResultBuilder();
		traceInfo = TraceInfo.Create(traceInfo);

		var jobExecuteResult = new JobExecuteResult(JobInstanceId, true, JobExecuteStatus.NONE);

		var transactionController = CreateTransactionController();

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionController,
				async (traceInfo, tc, unhandledExceptionDetail, cancellationToken) =>
				{
					Data = await JobRepository.LoadDataAsync<TData>(Name, tc, cancellationToken).ConfigureAwait(false);
					return result.Build();
				},
				$"{nameof(Job)}<{typeof(TData).FullName}> - {nameof(BeforeStartInternalAsync)}> Global exception",
				async (traceInfo, exception, detail) =>
				{
					var errorMessage =
						await Logger.LogErrorAsync(
							traceInfo,
							this,
							jobExecuteResult,
							null,
							LogCodes.LOAD_DATA_ERROR,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

					return errorMessage;
				},
				null,
				true,
				cancellationToken: default).ConfigureAwait(false);

		if (result.HasError())
			throw result.Build().ToException()!;
	}

	protected async Task SaveDataAsync(JobExecuteResult jobExecuteResult, ITraceInfo traceInfo, TData? data)
	{
		var result = new ResultBuilder();
		traceInfo = TraceInfo.Create(traceInfo);

		var transactionController = CreateTransactionController();

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionController,
				async (traceInfo, tc, unhandledExceptionDetail, cancellationToken) =>
				{
					await JobRepository.SaveDataAsync(Name, data, tc, cancellationToken).ConfigureAwait(false);

					tc.ScheduleCommit();

					return result.Build();
				},
				$"{nameof(Job)}<{typeof(TData).FullName}> - {nameof(BeforeStartInternalAsync)}> Global exception",
				async (traceInfo, exception, detail) =>
				{
					var errorMessage =
						await Logger.LogErrorAsync(
							traceInfo,
							this,
							jobExecuteResult,
							null,
							LogCodes.SAVE_DATA_ERROR,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

					return errorMessage;
				},
				null,
				true,
				cancellationToken: default).ConfigureAwait(false);

		if (result.HasError())
			throw result.Build().ToException()!;

		Data = data;
	}

	Task IJob<TData>.SaveDataInternalAsync(JobExecuteResult result, ITraceInfo traceInfo, TData? data)
		=> SaveDataAsync(result, traceInfo, data);

	public override T GetData<T>()
	{
		if (Data is T t)
			return t;

		return default!;
	}
}