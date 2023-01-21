namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for base request messages.
/// </summary>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessage
{
}
