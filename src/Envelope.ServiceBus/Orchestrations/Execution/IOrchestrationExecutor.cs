using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IOrchestrationExecutor
{
	IOrchestrationLogger OrchestrationLogger { get; }
	IOrchestrationHostOptions OrchestrationHostOptions { get; }

	Task ExecuteAsync(IOrchestrationInstance orchestrationInstance, ITraceInfo traceInfo);
}
