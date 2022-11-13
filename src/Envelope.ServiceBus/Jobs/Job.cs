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
using System.Threading;

namespace Envelope.ServiceBus.Jobs;

public abstract class Job : IJob
{
	private SequentialAsyncTimer? _sequentialAsyncTimer;
	private Timer? _periodicTimer;
	private CronAsyncTimer? _cronAsyncTimer;

	internal virtual Func<ITraceInfo, Task>? BeforeStartAsync { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected internal IServiceProvider MainServiceProvider { get; private set; }

	protected IHostInfo HostInfo { get; private set; }

	internal IJobRepository JobRepository { get; private set; }

	protected IJobLogger Logger { get; private set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private bool _cronAsyncTimerIsStopped = false;

	protected abstract IJobConfiguration Config { get; }

	public bool Initialized { get; private set; }

	public string Name => Config.Name;

	public bool Disabled => Config.Disabled;

	public JobExecutingMode Mode => Config.Mode;

	public TimeSpan? DelayedStart { get; protected set; }

	public TimeSpan? IdleTimeout { get; protected set; }

	public CronTimerSettings? CronTimerSettings { get; private set; }

	public JobStatus Status { get; private set; }

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

			MainServiceProvider = serviceProvider;
			HostInfo = config.HostInfoInternal ?? throw new InvalidOperationException($"{nameof(HostInfo)} == null");
			Logger = config.JobLogger(serviceProvider) ?? throw new InvalidOperationException($"{nameof(Logger)} == null");
			JobRepository = config.JobRepository(serviceProvider) ?? throw new InvalidOperationException($"{nameof(JobRepository)} == null"); ;

			DelayedStart = Config.DelayedStart;
			IdleTimeout = Config.IdleTimeout;
			CronTimerSettings = Config.CronTimerSettings;

			_cronAsyncTimerIsStopped = false;
			Status = Config.Disabled ? JobStatus.Disabled : JobStatus.Stopped;

			if (Status == JobStatus.Disabled)
				return;

			if (Mode == JobExecutingMode.SequentialIntervalTimer)
			{
				if (!IdleTimeout.HasValue)
					throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

				if (DelayedStart.HasValue)
					_sequentialAsyncTimer = new SequentialAsyncTimer(null, DelayedStart.Value, IdleTimeout.Value, OnSequentialAsyncTimerTickAsync, OnSequentialAsyncTimerExceptionAsync);
				else
					_sequentialAsyncTimer = new SequentialAsyncTimer(null, IdleTimeout.Value, OnSequentialAsyncTimerTickAsync, OnSequentialAsyncTimerExceptionAsync);
			}
			else if (Mode == JobExecutingMode.ExactPeriodicTimer)
			{
				if (!IdleTimeout.HasValue)
					throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

				if (DelayedStart.HasValue)
					_periodicTimer = new Timer(ExactTimerCallbackAsync, null, Timeout.Infinite, Timeout.Infinite);
				else
					_periodicTimer = new Timer(ExactTimerCallbackAsync, null, Timeout.Infinite, Timeout.Infinite);
			}
			else if (Mode == JobExecutingMode.Cron)
			{
				if (CronTimerSettings == null)
					throw new InvalidOperationException($"{nameof(CronTimerSettings)} == null");

				_cronAsyncTimer = new CronAsyncTimer(CronTimerSettings.CronExpression);
			}

			OnInitialize();

			Initialized = true;
		}
	}

	protected virtual void OnInitialize()
	{
	}

	void IJob.InitializeInternal(IJobProviderConfiguration config, IServiceProvider serviceProvider)
		=> Initialize(config, serviceProvider);

	private async Task<bool> OnSequentialAsyncTimerTickAsync(object? state)
	{
		Status = JobStatus.InProcess;
		await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
		var @continue = await ExecuteAsync(scopedServiceProvider.ServiceProvider).ConfigureAwait(false);

		if (!@continue)
			Status = JobStatus.Stopped;

		return @continue;
	}

	private async Task<bool> OnSequentialAsyncTimerExceptionAsync(object? state, Exception exception)
	{
		var @continue = await OnUnhandledExceptionAsync(TraceInfo.Create(MainServiceProvider), exception).ConfigureAwait(false);

		if (!@continue)
			Status = JobStatus.Stopped;

		return @continue;
	}

#pragma warning disable VSTHRD100 // Avoid async void methods
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
	private async void ExactTimerCallbackAsync(object? state)
	{
		bool @continue;
		Status = JobStatus.InProcess;

		try
		{
			await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
			@continue = await ExecuteAsync(scopedServiceProvider.ServiceProvider).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			@continue = await OnUnhandledExceptionAsync(TraceInfo.Create(MainServiceProvider), ex).ConfigureAwait(false);
		}

		if (!@continue)
			await StopAsync().ConfigureAwait(false);
	}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore VSTHRD100 // Avoid async void methods

	private async Task OnNextCronTimerTickAsync()
	{
		if (_cronAsyncTimerIsStopped)
			return;

		bool @continue;

		try
		{
			await using var scopedServiceProvider = MainServiceProvider.CreateAsyncScope();
			@continue = await ExecuteAsync(scopedServiceProvider.ServiceProvider).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			@continue = await OnUnhandledExceptionAsync(TraceInfo.Create(MainServiceProvider), ex).ConfigureAwait(false);
		}

		if (!@continue)
		{
			_cronAsyncTimerIsStopped = true;
			await StopAsync().ConfigureAwait(false);
		}
	}

	public async Task StartAsync(ITraceInfo traceInfo)
	{
		if (!Initialized)
			throw new InvalidOperationException("Not initialized");

		if (Status == JobStatus.Disabled)
			throw new InvalidOperationException($"Cannot start disabled job {Name}");

		if (Status != JobStatus.Stopped)
			return;

		if (BeforeStartAsync != null)
			await BeforeStartAsync.Invoke(traceInfo).ConfigureAwait(false);

		Status = JobStatus.Idle;

		if (Mode == JobExecutingMode.SequentialIntervalTimer)
		{
			await _sequentialAsyncTimer!.StartAsync().ConfigureAwait(false);
		}
		else if (Mode == JobExecutingMode.ExactPeriodicTimer)
		{
			if (!IdleTimeout.HasValue)
				throw new InvalidOperationException($"{nameof(IdleTimeout)} == null");

			if (DelayedStart.HasValue)
				_periodicTimer!.Change(DelayedStart.Value, IdleTimeout.Value);
			else
				_periodicTimer!.Change(TimeSpan.Zero, IdleTimeout.Value);
		}
		else if (Mode == JobExecutingMode.Cron)
		{
			_cronAsyncTimerIsStopped = false;

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
	}

	public async Task StopAsync()
	{
		if (!Initialized)
			throw new InvalidOperationException("Not initialized");

		if (Status == JobStatus.Disabled)
			throw new InvalidOperationException($"Cannot stop disabled job {Name}");

		if (Status == JobStatus.Stopped)
			return;

		Status = JobStatus.Stopped;

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
	}

	public abstract Task<bool> ExecuteAsync(IServiceProvider scopedServiceProvider);

	public virtual async Task<bool> OnUnhandledExceptionAsync(ITraceInfo traceInfo, Exception exception)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var detail = $"{this.GetType().FullName} >> {nameof(OnUnhandledExceptionAsync)}";
		await Logger.LogErrorAsync(traceInfo, Name, x => x.ExceptionInfo(exception).Detail(detail), detail, null, cancellationToken: default).ConfigureAwait(false);
		return true;
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
							Name,
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

	protected async Task SaveDataAsync(ITraceInfo traceInfo, TData? data)
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
							Name,
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

	Task IJob<TData>.SaveDataInternalAsync(ITraceInfo traceInfo, TData? data)
		=> SaveDataAsync(traceInfo, data);

	public override T GetData<T>()
	{
		if (Data is T t)
			return t;

		return default!;
	}
}