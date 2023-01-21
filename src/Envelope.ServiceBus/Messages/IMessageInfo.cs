namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessageInfo
{
	/// <summary>
	/// MessageId
	/// </summary>
	Guid MessageId { get; }

	/// <summary>
	/// The message can be processed only one.
	/// If False, the message can be processed.
	/// If True, the message was already processed.
	/// </summary>
	bool Processed { get; }
}
