using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class StepExecutionContext : IStepExecutionContext
{
	public ITraceInfo<Guid> TraceInfo { get; }

	public IOrchestrationInstance Orchestration { get; set; }

	public IOrchestrationStep Step { get; set; }

	public ExecutionPointer ExecutionPointer { get; set; }

	public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

	IOrchestrationInstance IStepExecutionContext.Orchestration => Orchestration;
	IOrchestrationStep IStepExecutionContext.Step => Step;
	IExecutionPointer IStepExecutionContext.ExecutionPointer => ExecutionPointer;

	public StepExecutionContext(IOrchestrationInstance orchestration, ExecutionPointer executionPointer, ITraceInfo<Guid> traceInfo)
	{
		TraceInfo = traceInfo ?? throw new ArgumentNullException(nameof(traceInfo));
		Orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
		ExecutionPointer = executionPointer ?? throw new ArgumentNullException(nameof(executionPointer));
		Step = ExecutionPointer.Step;
	}

	public TData GetData<TData>()
		=> (TData)Orchestration.Data;
}
