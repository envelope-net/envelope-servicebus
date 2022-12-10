using Envelope.Trace;

namespace Envelope.ServiceBus.Jobs;

public interface IJobController
{
	Task StartAllInternalAsync(ITraceInfo traceInfo);

	Task StopAllInternalAsync(ITraceInfo traceInfo);

	Task StartJobAsync(ITraceInfo traceInfo, string name);

	Task StopJobAsync(ITraceInfo traceInfo, string name);
}
