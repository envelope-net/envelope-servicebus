namespace Envelope.ServiceBus.Messages;

public class MessageMetadataUpdate : IMessageMetadataUpdate
{
	public Guid MessageId { get; }

	public bool Processed { get; set; }

	public MessageStatus MessageStatus { get; set; }

	public int RetryCount { get; set; }

	public DateTime? DelayedToUtc { get; set; }

	public MessageMetadataUpdate(Guid messageId)
	{
		MessageId = messageId;
	}

	public override string ToString()
		=> $"{nameof(Processed)} = {Processed} | {nameof(MessageStatus)} = {MessageStatus}{(DelayedToUtc.HasValue ? $"{nameof(DelayedToUtc)} = {DelayedToUtc}" : "")}";
}
