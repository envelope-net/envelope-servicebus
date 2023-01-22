namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ISavedMessage<out TMessage> : IMessageInfo
	where TMessage : class, IMessage
{
	TMessage? Message { get; }

	void SetMessageInternal(object message);
}
