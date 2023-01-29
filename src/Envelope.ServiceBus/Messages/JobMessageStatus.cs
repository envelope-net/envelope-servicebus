namespace Envelope.ServiceBus.Messages;

public enum JobMessageStatus
{
	Idle = 0,
	Completed = 1,
	Error = 2,
	Susspended = 3,
	Deleted = 4
}
