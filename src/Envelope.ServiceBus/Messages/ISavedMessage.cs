namespace Envelope.ServiceBus.Messages;

public interface ISavedMessage<TMessage> : IMessageInfo
	where TMessage : class, IMessage
{
	TMessage? Message { get; set; }
}
