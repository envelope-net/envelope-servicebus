using Envelope.Calendar;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration;

public interface IJobConfiguration : IValidable
{
	string Name { get; set; }

	bool Disabled { get; set; }

	JobExecutingMode Mode { get; set; }

	TimeSpan? DelayedStart { get; set; }

	TimeSpan? IdleTimeout { get; set; }

	CronTimerSettings? CronTimerSettings { get; set; }
}
