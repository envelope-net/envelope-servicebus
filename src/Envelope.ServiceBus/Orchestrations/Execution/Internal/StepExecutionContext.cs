using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class StepExecutionContext : IStepExecutionContext
{
	public ITraceInfo TraceInfo { get; }

	public IOrchestrationInstance Orchestration { get; set; }

	public IOrchestrationStep Step { get; set; }

	public ExecutionPointer ExecutionPointer { get; set; }

	public List<Guid> FinalizedBrancheIds { get; }

	public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

	IOrchestrationInstance IStepExecutionContext.Orchestration => Orchestration;
	IOrchestrationStep IStepExecutionContext.Step => Step;
	IExecutionPointer IStepExecutionContext.ExecutionPointer => ExecutionPointer;

	public StepExecutionContext(
		IOrchestrationInstance orchestration,
		ExecutionPointer executionPointer,
		List<Guid> finalizedBrancheIds,
		ITraceInfo traceInfo)
	{
		TraceInfo = traceInfo ?? throw new ArgumentNullException(nameof(traceInfo));
		Orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
		ExecutionPointer = executionPointer ?? throw new ArgumentNullException(nameof(executionPointer));
		Step = ExecutionPointer.GetStep();
		FinalizedBrancheIds = finalizedBrancheIds ?? new List<Guid>();
	}

	public TData GetData<TData>()
		=> (TData)Orchestration.Data;
}
