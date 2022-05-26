namespace Envelope.ServiceBus.DistributedCoordinator;

public interface IDistributedLockKeyFactory
{
	string DistributedLockResourceType { get; }

	string CreateDistributedLockKey();
}
