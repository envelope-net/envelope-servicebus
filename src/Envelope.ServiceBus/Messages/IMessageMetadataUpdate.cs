namespace Envelope.ServiceBus.Messages;

public interface IMessageMetadataUpdate
{
	Guid MessageId { get; }

	bool Processed { get; }

	MessageStatus MessageStatus { get; }

	int RetryCount { get; }

	DateTime? DelayedToUtc { get; }
}
