using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Model;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public class ExecutionPointer : IExecutionPointer
{
	public Guid IdExecutionPointer { get; }

	public IOrchestrationStep Step { get; }

	public bool Active { get; internal set; }

	public PointerStatus Status { get; internal set; }

	public DateTime? SleepUntilUtc { get; internal set; }

	public int RetryCount { get; internal set; }

	public DateTime? StartTimeUtc { get; internal set; }

	public DateTime? EndTimeUtc { get; internal set; }

	public string? EventName { get; internal set; }

	public string? EventKey { get; internal set; }

	public DateTime? EventWaitingTimeToLiveUtc { get; internal set; }

	public OrchestrationEvent? OrchestrationEvent { get; internal set; }

	public List<IExecutionPointer> NestedExecutionPointers { get; }

	public bool IsContainer => 0 < NestedExecutionPointers.Count;

	IReadOnlyList<IExecutionPointer> IExecutionPointer.NestedExecutionPointers => NestedExecutionPointers;

	public IExecutionPointer? PredecessorExecutionPointer { get; internal set; }

	public IExecutionPointer? ContainerExecutionPointer { get; internal set; }
	IExecutionPointer? IExecutionPointer.ContainerExecutionPointer { get => ContainerExecutionPointer; set => ContainerExecutionPointer = value; }

	internal ExecutionPointer(Guid idExecutionPointer, IOrchestrationStep step)
	{
		IdExecutionPointer = idExecutionPointer;
		Step = step ?? throw new ArgumentNullException(nameof(step));
		NestedExecutionPointers = new List<IExecutionPointer>();
		Status = PointerStatus.Pending;
	}

	internal void AddNestedExecutionPointer(IExecutionPointer nestedExecutionPointer)
	{
		if (nestedExecutionPointer == null)
			throw new ArgumentNullException(nameof(nestedExecutionPointer));

		NestedExecutionPointers.Add(nestedExecutionPointer);
		nestedExecutionPointer.ContainerExecutionPointer = this;
	}

	void IExecutionPointer.AddNestedExecutionPointer(IExecutionPointer nestedExecutionPointer)
		=> AddNestedExecutionPointer(nestedExecutionPointer);

	public override string ToString()
		=> Step.ToString()!;
}
