using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Model;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public class ExecutionPointer : IExecutionPointer
{
	private readonly IOrchestrationStep _step;

	public Guid IdExecutionPointer { get; }

	public Guid IdOrchestrationInstance { get; }

	public Guid IdOrchestrationDefinition { get; }

	public int OrchestrationInstanceVersion { get; }

	public Guid IdStep { get; }

	public bool Active { get; private set; }

	public PointerStatus Status { get; private set; }

	public DateTime? SleepUntilUtc { get; private set; }

	public int RetryCount { get; private set; }

	public DateTime? StartTimeUtc { get; private set; }

	public DateTime? EndTimeUtc { get; private set; }

	public string? EventName { get; private set; }

	public string? EventKey { get; private set; }

	public DateTime? EventWaitingTimeToLiveUtc { get; private set; }

	public OrchestrationEvent? OrchestrationEvent { get; private set; }

	public Guid? PredecessorExecutionPointerId { get; private set; }

	public Guid? PredecessorExecutionPointerStartingStepId { get; private set; }

	internal ExecutionPointer(
		Guid idExecutionPointer,
		Guid idOrchestrationInstance,
		Guid idOrchestrationDefinition,
		int version,
		IOrchestrationStep step)
	{
		IdExecutionPointer = idExecutionPointer;
		IdOrchestrationInstance = idOrchestrationInstance;
		IdOrchestrationDefinition = idOrchestrationDefinition;
		OrchestrationInstanceVersion = version;
		_step = step ?? throw new ArgumentNullException(nameof(step));
		IdStep = _step.IdStep;
		Status = PointerStatus.Pending;
	}

	internal ExecutionPointer Update(IExecutionPointerUpdate update)
	{
		if (update == null)
			throw new ArgumentNullException(nameof(update));

		if (update.SetActive)
			Active = update.Active;

		if (update.SetStatus)
			Status = update.Status;

		if (update.SetSleepUntilUtc)
			SleepUntilUtc = update.SleepUntilUtc;

		if (update.SetRetryCount)
			RetryCount = update.RetryCount;

		if (update.SetStartTimeUtc)
			StartTimeUtc = update.StartTimeUtc;

		if (update.SetEndTimeUtc)
			EndTimeUtc = update.EndTimeUtc;

		if (update.SetEventName)
			EventName = update.EventName;

		if (update.SetEventKey)
			EventKey = update.EventKey;

		if (update.SetEventWaitingTimeToLiveUtc)
			EventWaitingTimeToLiveUtc = update.EventWaitingTimeToLiveUtc;

		if (update.SetOrchestrationEvent)
			OrchestrationEvent = update.OrchestrationEvent;

		if (update.SetPredecessorExecutionPointerId)
			PredecessorExecutionPointerId = update.PredecessorExecutionPointerId;

		if (update.SetPredecessorExecutionPointerStartingStepId)
			PredecessorExecutionPointerStartingStepId = update.PredecessorExecutionPointerStartingStepId;

		return this;
	}

	ExecutionPointer IExecutionPointer.Update(IExecutionPointerUpdate update)
		=> Update(update);

	public IOrchestrationStep GetStep()
		=> _step;

	public override string ToString()
		=> _step.ToString()!;
}
