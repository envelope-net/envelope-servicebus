using Envelope.Converters;
using Envelope.Infrastructure;

namespace Envelope.ServiceBus.Hosts;

public class HostInfo : IHostInfo
{
	public string HostName { get; }

	public Guid HostId { get; }

	public Guid InstanceId { get; }

	public EnvironmentInfo EnvironmentInfo { get; }

	public HostInfo(string name)
	{
		HostName = !string.IsNullOrWhiteSpace(name)
			? name
			: throw new ArgumentNullException(nameof(name));

		HostId = GuidConverter.ToGuid(HostName);
		InstanceId = Guid.NewGuid();
		EnvironmentInfo = EnvironmentInfoProvider.GetEnvironmentInfo(HostName);
	}
}
