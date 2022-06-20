namespace Envelope.ServiceBus.Queues;

public enum QueueStatus
{
	/// <summary>
	/// Enabled Write and enabled Read
	/// </summary>
	Running = 0,

	/// <summary>
	/// Enbaled Write and disabled Read
	/// </summary>
	Suspended = 1,

	/// <summary>
	/// Disabled Write and disabled Read
	/// </summary>
	Terminated = 2
}
