namespace Envelope.ServiceBus.Jobs;

public enum JobExecuteStatus
{
	NONE = 0, //min !!!
	Disabled = 1,
	Running = 2,
	Succeeded = 3,
	WithWarnings = 4,
	Failed = 5,
	Invalid = 6 //max !!!
}
