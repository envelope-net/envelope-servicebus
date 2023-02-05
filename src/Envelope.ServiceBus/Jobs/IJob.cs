using Envelope.Calendar;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.Trace;

namespace Envelope.ServiceBus.Jobs;

public interface IJob
{
	Guid JobInstanceId { get; }

	IHostInfo HostInfo { get; }

	string Name { get; }

	string? Description { get; }

	bool Disabled { get; }

	JobExecutingMode Mode { get; }

	TimeSpan? DelayedStart { get; }

	TimeSpan? IdleTimeout { get; }

	CronTimerSettings? CronTimerSettings { get; }

	DateTime? NextExecutionRunUtc { get; }

	int ExecutionEstimatedTimeInSeconds { get; }

	int DeclaringAsOfflineAfterMinutesOfInactivity { get; }

	DateTime LastUpdateUtc { get; }

	DateTime? LastExecutionStartedUtc { get; }

	JobStatus Status { get; }

	IReadOnlyDictionary<int,string>? JobExecutionOperations { get; }

	IReadOnlyList<int>? AssociatedJobMessageTypes { get; }

	Task StartAsync(ITraceInfo traceInfo);

	Task StopAsync(ITraceInfo traceInfo);

	void InitializeInternal(IJobProviderConfiguration config, IServiceProvider serviceProvider);

	T? GetData<T>();
}

public interface IJob<TData> : IJob
{
	TData? Data { get; }

	Task SaveDataInternalAsync(JobExecuteResult result, ITraceInfo traceInfo, TData? data);
}
