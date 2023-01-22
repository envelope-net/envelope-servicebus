using Envelope.Infrastructure;

namespace Envelope.ServiceBus.Hosts;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IHostInfo
{
	string HostName { get; }

	/// <summary>
	/// Host identifier based on Name
	/// </summary>
	Guid HostId { get; }

	Guid InstanceId { get; }

	HostStatus HostStatus { get; }

	EnvironmentInfo EnvironmentInfo { get; }
}
