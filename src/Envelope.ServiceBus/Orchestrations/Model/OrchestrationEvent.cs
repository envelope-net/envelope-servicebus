using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public class OrchestrationEvent : IEvent
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public Guid Id { get; set; }

	public string EventName { get; set; }

	public string OrchestrationKey { get; set; }

	public object EventData { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public DateTime EventTime { get; set; }

	public DateTime? ProcessedUtc { get; set; }
}
