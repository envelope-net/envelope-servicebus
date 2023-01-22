using Envelope.ServiceBus.Hosts;

namespace Envelope.ServiceBus.Queries;

public interface IDbHost
{
	Guid HostId { get; }
	IHostInfo HostInfo { get; }
	int HostStatus { get; }
	DateTime LastUpdateUtc { get; }

	string ToJson();
}
