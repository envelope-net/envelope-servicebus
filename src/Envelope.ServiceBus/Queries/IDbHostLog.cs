using Envelope.Logging;

namespace Envelope.ServiceBus.Queries;

public interface IDbHostLog
{
	Guid IdLogMessage { get; }
	ILogMessage LogMessage { get; }
	int IdLogLevel { get; }
	Guid HostId { get; }
	Guid HostInstanceId { get; }
	DateTime CreatedUtc { get; }

	string ToJson();
}
