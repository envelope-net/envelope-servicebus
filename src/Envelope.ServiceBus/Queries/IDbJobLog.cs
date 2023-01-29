using Envelope.Logging;

namespace Envelope.ServiceBus.Queries;

public interface IDbJobLog
{
	Guid IdLogMessage { get; }
	Guid JobInstanceId { get; }
	string? Detail { get; }
	Guid ExecutionId { get; }
	string LogCode { get; }
	ILogMessage LogMessage { get; }
	int IdLogLevel { get; }
	int Status { get; }
	int ExecuteStatus { get; }
	DateTime CreatedUtc { get; }
	Guid? JobMessageId { get; }

	string ToJson();
}
