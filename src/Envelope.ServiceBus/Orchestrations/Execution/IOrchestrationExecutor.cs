using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IOrchestrationExecutor
{
	Task ExecuteAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo);
}
