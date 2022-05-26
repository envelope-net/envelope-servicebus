namespace Envelope.ServiceBus.Queues;

public enum QueueType
{
	/// <summary>
	/// One by one, like FIFO, but messages with delay timestamp, can be skipped,
	/// and when the delay elapses, this message is on turn
	/// </summary>
	Sequential_Delayable = 0,

	/// <summary>
	/// First in, first out - each message is waiting for the previous message to complete
	/// </summary>
	Sequential_FIFO = 1,

	Parallel = 2
}
