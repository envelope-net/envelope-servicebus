using Envelope.Infrastructure;

namespace Envelope.ServiceBus.Hosts;

public interface IHostInfo
{
	string HostName { get; }

	/// <summary>
	/// Host identifier based on Name
	/// </summary>
	Guid HostId { get; }

	Guid InstanceId { get; }

	EnvironmentInfo EnvironmentInfo { get; }
}
