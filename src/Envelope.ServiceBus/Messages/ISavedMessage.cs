namespace Envelope.ServiceBus.Messages;

public interface ISavedMessage<out TMessage> : IMessageInfo
	where TMessage : class, IMessage
{
	TMessage? Message { get; }

	void SetMessageInternal(object message);
}
