namespace Envelope.ServiceBus.DistributedCoordinator;

/// <remarks>
/// The implemention of this interface will be responsible for
/// providing a (distributed) locking mechanism to manage in flight orchestrations    
/// </remarks>
public interface IDistributedLockProvider
{
	/// <summary>
	/// Acquire a lock on the specified resource.
	/// </summary>
	/// <param name="distributedLockKeyProvider">Resource key to lock.</param>
	/// <param name="owner"></param>
	/// <param name="expirationUtc"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>`true`, if the lock was acquired.</returns>
	Task<LockResult> AcquireLockAsync(
		IDistributedLockKeyFactory distributedLockKeyProvider,
		string owner,
		DateTime expirationUtc,
		CancellationToken cancellationToken);

	/// <summary>
	/// v ramci releasovania rozdistribuuj === zosynchronizuj aj contextove data, ktore sa zmenili, pocas toho co bol aktivny lock
	/// </summary>
	Task<LockResult> ReleaseLockAsync(IDistributedLockKeyFactory distributedLockKeyProvider, ISyncData syncData);
}
