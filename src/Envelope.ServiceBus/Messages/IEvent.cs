namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IEvent : IMessage
{
}
