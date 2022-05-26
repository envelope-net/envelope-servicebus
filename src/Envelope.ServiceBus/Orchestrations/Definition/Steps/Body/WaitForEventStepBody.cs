using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

internal class WaitForEventStepBody : ISyncStepBody, IStepBody
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public string EventName { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public BodyType BodyType => BodyType.WaitForEvent;

	public string? EventKey { get; set; }

	public DateTime? TimeToLiveUtc { get; set; }

	public IExecutionResult Run(IStepExecutionContext context)
		=> context.ExecutionPointer.OrchestrationEvent == null
			? ExecutionResultFactory.WaitForEvent(EventName, EventKey, TimeToLiveUtc)
			: ExecutionResultFactory.NextStep();
}
