using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Jobs;

public interface IJobRegister
{
	ConcurrentDictionary<string, IJob> JobsInternal { get; }

	void RegisterJob(IJob job);

	Task RegisterJobAsync<TData>(IJob<TData> job, TData? data, ITraceInfo traceInfo);
}
