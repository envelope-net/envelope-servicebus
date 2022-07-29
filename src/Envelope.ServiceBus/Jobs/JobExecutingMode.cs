namespace Envelope.ServiceBus.Jobs;

public enum JobExecutingMode
{
	SequentialIntervalTimer = 0,
	ExactPeriodicTimer = 1,
	Cron = 2
}
