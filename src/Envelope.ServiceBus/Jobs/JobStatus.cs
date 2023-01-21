namespace Envelope.ServiceBus.Jobs;

public enum JobStatus
{
	Disabled = 0,
	Stopped = 1,
	Idle = 2,
	InProcess = 3,
	TooLongProcessing = 4, //calculated by last execution
	Offline = 5 //calculated by last execution
}
