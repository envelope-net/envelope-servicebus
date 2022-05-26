using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IStepExecutionContext
{
	ITraceInfo<Guid> TraceInfo { get; }

	IExecutionPointer ExecutionPointer { get; }

	IOrchestrationStep Step { get; }

	IOrchestrationInstance Orchestration { get; }

	CancellationToken CancellationToken { get; }

	TData GetData<TData>();
}
