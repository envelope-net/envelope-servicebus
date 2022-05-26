namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class ExecutionResult : IExecutionResult
{
	public List<Guid>? NextSteps { get; set; }
	IReadOnlyList<Guid>? IExecutionResult.NextSteps => NextSteps;

	public List<Guid>? BranchSteps { get; set; }
	IReadOnlyList<Guid>? IExecutionResult.NestedSteps => BranchSteps;

	public bool IsError { get; set; }

	public bool Retry { get; set; }

	public TimeSpan? RetryInterval { get; set; }

	public string? EventName { get; set; }

	public string? EventKey { get; set; }

	public DateTime? EventWaitingTimeToLiveUtc { get; set; }
}
