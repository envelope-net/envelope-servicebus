using Envelope.Converters;
using Envelope.Infrastructure;

namespace Envelope.ServiceBus.Hosts;

public class HostInfo : IHostInfo
{
	public string HostName { get; }

	public Guid HostId { get; }

	public Guid InstanceId { get; }

	public HostStatus HostStatus { get; set; }

	public EnvironmentInfo EnvironmentInfo { get; }

	public HostInfo(string name)
	{
		HostName = !string.IsNullOrWhiteSpace(name)
			? name
			: throw new ArgumentNullException(nameof(name));

		HostId = GuidConverter.ToGuid(HostName);
		InstanceId = Guid.NewGuid();
		HostStatus = HostStatus.Online;
		EnvironmentInfo = EnvironmentInfoProvider.GetEnvironmentInfo(HostName);
	}
}
