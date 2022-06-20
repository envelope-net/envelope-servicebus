using Envelope.ServiceBus.Orchestrations.Model;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public class ExecutionPointerUpdate : IExecutionPointerUpdate
{
	private bool active;
	private PointerStatus status;
	private DateTime? sleepUntilUtc;
	private int retryCount;
	private DateTime? startTimeUtc;
	private DateTime? endTimeUtc;
	private string? eventName;
	private string? eventKey;
	private DateTime? eventWaitingTimeToLiveUtc;
	private OrchestrationEvent? orchestrationEvent;
	private Guid? predecessorExecutionPointerId;
	private Guid? predecessorExecutionPointerStartingStepId;

	public Guid IdExecutionPointer { get; }

	public bool SetActive { get; private set; }
	public bool Active { get => active; set { active = value; SetActive = true; } }

	public bool SetStatus { get; private set; }
	public PointerStatus Status { get => status; set { status = value; SetStatus = true; } }

	public bool SetSleepUntilUtc { get; private set; }
	public DateTime? SleepUntilUtc { get => sleepUntilUtc; set { sleepUntilUtc = value; SetSleepUntilUtc = true; } }

	public bool SetRetryCount { get; private set; }
	public int RetryCount { get => retryCount; set { retryCount = value; SetRetryCount = true; } }

	public bool SetStartTimeUtc { get; private set; }
	public DateTime? StartTimeUtc { get => startTimeUtc; set { startTimeUtc = value; SetStartTimeUtc = true; } }

	public bool SetEndTimeUtc { get; private set; }
	public DateTime? EndTimeUtc { get => endTimeUtc; set { endTimeUtc = value; SetEndTimeUtc = true; } }

	public bool SetEventName { get; private set; }
	public string? EventName { get => eventName; set { eventName = value; SetEventName = true; } }

	public bool SetEventKey { get; private set; }
	public string? EventKey { get => eventKey; set { eventKey = value; SetEventKey = true; } }

	public bool SetEventWaitingTimeToLiveUtc { get; private set; }
	public DateTime? EventWaitingTimeToLiveUtc { get => eventWaitingTimeToLiveUtc; set { eventWaitingTimeToLiveUtc = value; SetEventWaitingTimeToLiveUtc = true; } }

	public bool SetOrchestrationEvent { get; private set; }
	public OrchestrationEvent? OrchestrationEvent { get => orchestrationEvent; set { orchestrationEvent = value; SetOrchestrationEvent = true; } }

	public bool SetPredecessorExecutionPointerId { get; private set; }
	public Guid? PredecessorExecutionPointerId { get => predecessorExecutionPointerId; set { predecessorExecutionPointerId = value; SetPredecessorExecutionPointerId = true; } }

	public bool SetPredecessorExecutionPointerStartingStepId { get; private set; }
	public Guid? PredecessorExecutionPointerStartingStepId { get => predecessorExecutionPointerStartingStepId; set { predecessorExecutionPointerStartingStepId = value; SetPredecessorExecutionPointerStartingStepId = true; } }

	public ExecutionPointerUpdate(Guid idExecutionPointer)
	{
		IdExecutionPointer = idExecutionPointer;
	}
}
