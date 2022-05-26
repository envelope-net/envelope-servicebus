namespace Envelope.ServiceBus.Orchestrations;

public enum OrchestrationStatus
{
	Running = 0,
	Executing = 1,
	Suspended = 2,
	Completed = 3,
	Terminated = 4,
}
