namespace Envelope.ServiceBus.Queries;

public interface IDbJobExecution
{
	Guid ExecutionId { get; }
	string JobName { get; }
	Guid JobInstanceId { get; }
	int ExecuteStatus { get; }
	DateTime StartedUtc { get; }
	DateTime? FinishedUtc { get; }

	string ToJson();
}
