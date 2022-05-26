namespace Envelope.ServiceBus.DistributedCoordinator;

public class LockResult
{
	public bool Succeeded { get; }
	public string? LockedBy { get; }

	public LockResult(bool succeeded, string? lockedBy)
	{
		Succeeded = succeeded;
		LockedBy = lockedBy;
	}
}
