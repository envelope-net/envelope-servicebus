using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Model;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IExecutionPointer
{
	Guid IdExecutionPointer { get; }

	IOrchestrationStep Step { get; }

	bool Active { get; }

	DateTime? SleepUntilUtc { get; }

	int RetryCount { get; }

	DateTime? StartTimeUtc { get; }

	DateTime? EndTimeUtc { get; }

	string? EventName { get; }

	string? EventKey { get; }

	DateTime? EventWaitingTimeToLiveUtc { get; }

	OrchestrationEvent? OrchestrationEvent { get; }

	IReadOnlyList<IExecutionPointer> NestedExecutionPointers { get; }

	bool IsContainer { get; }

	IExecutionPointer? PredecessorExecutionPointer { get; }

	IExecutionPointer? ContainerExecutionPointer { get; internal set; }

	PointerStatus Status { get; }

	internal void AddNestedExecutionPointer(IExecutionPointer nestedExecutionPointer);
}
