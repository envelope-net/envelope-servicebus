using Envelope.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace Envelope.ServiceBus.DistributedCoordinator.Internal;

/// <summary>
/// In-memory implementation of <see cref="IDistributedLockProvider"/>
/// </summary>
internal class InMemoryLockProvider : IDistributedLockProvider
{
	private readonly AsyncLock _locker = new();
	private readonly IMemoryCache _cache;

	public InMemoryLockProvider()
	{
		_cache = new MemoryCache(new MemoryCacheOptions());
	}

	public async Task<LockResult> AcquireLockAsync(
		IDistributedLockKeyFactory distributedLockKeyProvider,
		string owner,
		DateTime expirationUtc,
		CancellationToken cancellationToken)
	{
		if (distributedLockKeyProvider == null)
			throw new ArgumentNullException(nameof(distributedLockKeyProvider));

		if (string.IsNullOrWhiteSpace(owner))
			throw new ArgumentNullException(nameof(owner));

		var key = distributedLockKeyProvider.CreateDistributedLockKey();

		using (await _locker.LockAsync().ConfigureAwait(false))
		{
			if (_cache.TryGetValue(key, out IDistributedLock distributedLock) && distributedLock.Owner != owner)
				return new LockResult(false, distributedLock.Owner);

			_cache.Set(key, new DistributedLock { ResourceType = distributedLockKeyProvider.DistributedLockResourceType, Key = key, Owner = owner, ExpireUtc = expirationUtc }, expirationUtc);
			return new LockResult(true, null);
		}
	}

	public async Task<LockResult> ReleaseLockAsync(IDistributedLockKeyFactory distributedLockKeyProvider, ISyncData syncData)
	{
		if (distributedLockKeyProvider == null)
			throw new ArgumentNullException(nameof(distributedLockKeyProvider));

		if (syncData == null)
			throw new ArgumentNullException(nameof(syncData));

		var key = distributedLockKeyProvider.CreateDistributedLockKey();

		using (await _locker.LockAsync().ConfigureAwait(false))
		{
			if (_cache.TryGetValue(key, out IDistributedLock distributedLock) && distributedLock.Owner != syncData.Owner)
				return new LockResult(false, distributedLock.Owner);

			_cache.Remove(key);
			return new LockResult(true, null);
		}
	}
}
