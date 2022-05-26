namespace Envelope.ServiceBus.Messages;

public interface IMessageMetadataUpdate
{
	Guid MessageId { get; }

	bool Processed { get; set; }

	MessageStatus MessageStatus { get; set; }

	int RetryCount { get; set; }

	DateTime? DelayedToUtc { get; set; }
}
