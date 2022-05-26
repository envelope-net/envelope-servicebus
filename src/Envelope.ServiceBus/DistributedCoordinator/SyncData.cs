namespace Envelope.ServiceBus.DistributedCoordinator;

public class SyncData : ISyncData
{
	public string Owner { get; set; }

	public SyncData(string owner)
	{
		if (string.IsNullOrWhiteSpace(owner))
			throw new ArgumentNullException(nameof(owner));

		Owner = owner;
	}
}
