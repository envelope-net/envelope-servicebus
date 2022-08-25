using Envelope.Calendar;
using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.Trace;

namespace Envelope.ServiceBus.Jobs;

public interface IJob
{
	string Name { get; }

	bool Disabled { get; }

	JobExecutingMode Mode { get; }

	TimeSpan? DelayedStart { get; }

	TimeSpan? IdleTimeout { get; }

	CronTimerSettings? CronTimerSettings { get; }

	JobStatus Status { get; }

	Task StartAsync(ITraceInfo traceInfo);

	Task StopAsync();

	void InitializeInternal(IJobProviderConfiguration config, IServiceProvider serviceProvider);
}

public interface IJob<TData> : IJob
{
	TData? Data { get; }

	Task SetDataInternalAsync(ITraceInfo traceInfo, TData data);
}
