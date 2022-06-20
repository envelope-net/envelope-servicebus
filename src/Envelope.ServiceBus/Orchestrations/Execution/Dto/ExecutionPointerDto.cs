using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Model;

namespace Envelope.ServiceBus.Orchestrations.Execution.Dto;

public class ExecutionPointerDto
{
	public Guid IdExecutionPointer { get; set; }

	public Guid IdOrchestrationInstance { get; set; }

	public Guid IdOrchestrationDefinition { get; set; }

	public int OrchestrationInstanceVersion { get; set; }

	public Guid IdStep { get; set; }

	public bool Active { get; set; }

	public PointerStatus Status { get; set; }

	public DateTime? SleepUntilUtc { get; set; }

	public int RetryCount { get; set; }

	public DateTime? StartTimeUtc { get; set; }

	public DateTime? EndTimeUtc { get; set; }

	public string? EventName { get; set; }

	public string? EventKey { get; set; }

	public DateTime? EventWaitingTimeToLiveUtc { get; set; }

	public OrchestrationEvent? OrchestrationEvent { get; set; }

	public Guid? PredecessorExecutionPointerId { get; set; }

	public Guid? PredecessorExecutionPointerStartingStepId { get; set; }

	public ExecutionPointerDto()
	{
	}

	public ExecutionPointerDto(ExecutionPointer executionPointer)
	{
		if (executionPointer  == null)
			throw new ArgumentNullException(nameof(executionPointer));

		IdExecutionPointer = executionPointer.IdExecutionPointer;
		IdOrchestrationInstance = executionPointer.IdOrchestrationInstance;
		IdOrchestrationDefinition = executionPointer.IdOrchestrationDefinition;
		OrchestrationInstanceVersion = executionPointer.OrchestrationInstanceVersion;
		IdStep = executionPointer.IdStep;
		Active = executionPointer.Active;
		Status = executionPointer.Status;
		SleepUntilUtc = executionPointer.SleepUntilUtc;
		RetryCount = executionPointer.RetryCount;
		StartTimeUtc = executionPointer.StartTimeUtc;
		EndTimeUtc = executionPointer.EndTimeUtc;
		EventName = executionPointer.EventName;
		EventKey = executionPointer.EventKey;
		EventWaitingTimeToLiveUtc = executionPointer.EventWaitingTimeToLiveUtc;
		OrchestrationEvent = executionPointer.OrchestrationEvent;
		PredecessorExecutionPointerId = executionPointer.PredecessorExecutionPointerId;
		PredecessorExecutionPointerStartingStepId = executionPointer.PredecessorExecutionPointerStartingStepId;
	}

	public ExecutionPointerDto Update(IExecutionPointerUpdate update)
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

	public ExecutionPointer ToExecutionPointer(IOrchestrationStep step)
	{
		var pointer = new ExecutionPointer(IdExecutionPointer, IdOrchestrationInstance, IdOrchestrationDefinition, OrchestrationInstanceVersion, step)
			.Update(new ExecutionPointerUpdate(IdExecutionPointer)
			{
				Active = Active,
				Status = Status,
				SleepUntilUtc = SleepUntilUtc,
				RetryCount = RetryCount,
				StartTimeUtc = StartTimeUtc,
				EndTimeUtc = EndTimeUtc,
				EventName = EventName,
				EventKey = EventKey,
				EventWaitingTimeToLiveUtc = EventWaitingTimeToLiveUtc,
				OrchestrationEvent = OrchestrationEvent,
				PredecessorExecutionPointerId = PredecessorExecutionPointerId,
				PredecessorExecutionPointerStartingStepId = PredecessorExecutionPointerStartingStepId
			});

		return pointer;
	}
}
