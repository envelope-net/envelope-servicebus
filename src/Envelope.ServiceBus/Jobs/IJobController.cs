using Envelope.Trace;

namespace Envelope.ServiceBus.Jobs;

public interface IJobController
{
	internal Task StartAllAsync(ITraceInfo traceInfo);

	internal Task StopAllAsync(ITraceInfo traceInfo);

	Task StartJobAsync(ITraceInfo traceInfo, string name);

	Task StopJobAsync(string name);
}
