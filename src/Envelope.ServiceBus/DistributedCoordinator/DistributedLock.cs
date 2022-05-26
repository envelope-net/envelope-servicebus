namespace Envelope.ServiceBus.DistributedCoordinator;

public class DistributedLock : IDistributedLock
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public string ResourceType { get; set; }

	public string Key { get; set; }

	public string Owner { get; set; }

	public DateTime ExpireUtc { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
