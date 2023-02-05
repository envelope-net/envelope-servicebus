using Envelope.ServiceBus.Jobs;

namespace Envelope.ServiceBus.Queries;

public interface IDbJob
{
	Guid JobInstanceId { get; }
	Guid HostInstanceId { get; }
	string HostName { get; }
	string Name { get; }
	string? Description { get; }
	bool Disabled { get; }
	int Mode { get; }
	TimeSpan? DelayedStart { get; }
	TimeSpan? IdleTimeout { get; }
	string? CronExpression { get; }
	bool CronExpressionIncludeSeconds { get; }
	DateTime? NextExecutionRunUtc { get; }
	int Status { get; }
	IReadOnlyDictionary<int, string>? JobExecutionOperations { get; }
	IReadOnlyList<int>? AssociatedJobMessageTypes { get; }
	int CurrentExecuteStatus { get; }
	int ExecutionEstimatedTimeInSeconds { get; }
	int DeclaringAsOfflineAfterMinutesOfInactivity { get; }
	DateTime LastUpdateUtc { get; }
	DateTime? LastExecutionStartedUtc { get; }

	string ToJson();

	JobStatus GetJobActivityStatus();
}
