using Envelope.Calendar;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Timers;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs;

public abstract class Job : IJob
{
	private SequentialAsyncTimer? _sequentialAsyncTimer;
	private Timer? _periodicTimer;
	private CronAsyncTimer? _cronAsyncTimer;

	internal virtual Func<ITraceInfo, Task>? BeforeStartAsync { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected IServiceProvider ServiceProvider { get; private set; }

	protected IHostInfo HostInfo { get; private set; }

	protected ITransactionManagerFactory TransactionManagerFactory { get; private set; }

	protected Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; private set; }

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

			ServiceProvider = serviceProvider;
			HostInfo = config.HostInfo ?? throw new InvalidOperationException($"{nameof(HostInfo)} == null");
			TransactionManagerFactory = config.TransactionManagerFactory ?? throw new InvalidOperationException($"{nameof(TransactionManagerFactory)} == null");
			TransactionContextFactory = config.TransactionContextFactory ?? throw new InvalidOperationException($"{nameof(TransactionContextFactory)} == null");
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

			Initialized = true;
		}
	}

	void IJob.Initialize(IJobProviderConfiguration config, IServiceProvider serviceProvider)
		=> Initialize(config, serviceProvider);

	private async Task<bool> OnSequentialAsyncTimerTickAsync(object? state)
	{
		Status = JobStatus.InProcess;
		var @continue = await ExecuteAsync();

		if (!@continue)
			Status = JobStatus.Stopped;

		return @continue;
	}

	private async Task<bool> OnSequentialAsyncTimerExceptionAsync(object? state, Exception exception)
	{
		var @continue = await OnUnhandledExceptionAsync(TraceInfo.Create(ServiceProvider), exception);

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
			@continue = await ExecuteAsync();
		}
		catch (Exception ex)
		{
			@continue = await OnUnhandledExceptionAsync(TraceInfo.Create(ServiceProvider), ex);
		}

		if (!@continue)
			await StopAsync();
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
			@continue = await ExecuteAsync();
		}
		catch (Exception ex)
		{
			@continue = await OnUnhandledExceptionAsync(TraceInfo.Create(ServiceProvider), ex);
		}

		if (!@continue)
		{
			_cronAsyncTimerIsStopped = true;
			await StopAsync();
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
			await BeforeStartAsync.Invoke(traceInfo);

		Status = JobStatus.Idle;

		if (Mode == JobExecutingMode.SequentialIntervalTimer)
		{
			await _sequentialAsyncTimer!.StartAsync();
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

			while (await _cronAsyncTimer!.WaitForNextTickAsync())
			{
				await OnNextCronTimerTickAsync();
			}
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
			await _sequentialAsyncTimer!.StopAsync();
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

	public abstract Task<bool> ExecuteAsync();

	public virtual async Task<bool> OnUnhandledExceptionAsync(ITraceInfo traceInfo, Exception exception)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var detail = $"{this.GetType().FullName} >> {nameof(OnUnhandledExceptionAsync)}";
		await Logger.LogErrorAsync(traceInfo, Name, x => x.ExceptionInfo(exception).Detail(detail), detail, null, cancellationToken: default);
		return true;
	}
}

public abstract class Job<TData> : Job, IJob<TData>, IJob
{
	public TData? Data { get; private set; }

	internal override Func<ITraceInfo, Task>? BeforeStartAsync => BeforeStartInternalAsync;

	private async Task BeforeStartInternalAsync(ITraceInfo traceInfo)
	{
		var result = new ResultBuilder();
		traceInfo = TraceInfo.Create(traceInfo);

		var transactionManager = TransactionManagerFactory.Create();
		var transactionContext = await TransactionContextFactory(ServiceProvider, transactionManager).ConfigureAwait(false);

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionContext,
				async (traceInfo, tc, cancellationToken) =>
				{
					Data = await JobRepository.LoadDataAsync<TData>(Name, tc, cancellationToken);
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

	internal protected async Task SetDataAsync(ITraceInfo traceInfo, TData data)
	{
		var result = new ResultBuilder();
		traceInfo = TraceInfo.Create(traceInfo);

		var transactionManager = TransactionManagerFactory.Create();
		var transactionContext = await TransactionContextFactory(ServiceProvider, transactionManager).ConfigureAwait(false);

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionContext,
				async (traceInfo, tc, cancellationToken) =>
				{
					await JobRepository.SaveDataAsync<TData>(Name, data, tc, cancellationToken);
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

	Task IJob<TData>.SetDataAsync(ITraceInfo traceInfo, TData data)
		=> SetDataAsync(traceInfo, data);
}