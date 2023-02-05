using Envelope.Calendar;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IJobConfiguration : IValidable
{
	string Name { get; set; }

	string? Description { get; set; }

	bool Disabled { get; set; }

	JobExecutingMode Mode { get; set; }

	TimeSpan? DelayedStart { get; set; }

	TimeSpan? IdleTimeout { get; set; }

	bool CronStartImmediately { get; set; }

	CronTimerSettings? CronTimerSettings { get; set; }

	int ExecutionEstimatedTimeInSeconds { get; set; }

	int DeclaringAsOfflineAfterMinutesOfInactivity { get; set; }

	Dictionary<int, string>? JobExecutionOperations { get; set; }

	List<int>? AssociatedJobMessageTypes { get; set; }
}
