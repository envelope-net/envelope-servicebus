namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessageMetadataUpdate
{
	Guid MessageId { get; }

	bool Processed { get; }

	MessageStatus MessageStatus { get; }

	int RetryCount { get; }

	DateTime? DelayedToUtc { get; }
}
