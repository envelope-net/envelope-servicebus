namespace Envelope.ServiceBus.DistributedCoordinator;

public interface IDistributedLock
{
	string ResourceType { get; }

	/// <summary>
	/// Target object lock key
	/// </summary>
	public string Key { get; }

	/// <summary>
	/// The caller, lock owner
	/// </summary>
	public string Owner { get; }

	/// <summary>
	/// Lock timeout
	/// </summary>
	public DateTime ExpireUtc { get; }
}
