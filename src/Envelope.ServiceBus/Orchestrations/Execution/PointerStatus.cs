namespace Envelope.ServiceBus.Orchestrations.Execution;

public enum PointerStatus
{
	Pending = 0,
	InProcess = 1,
	Completed = 2,
	Retrying = 3,
	WaitingForEvent = 4,
	Suspended = 5
}
