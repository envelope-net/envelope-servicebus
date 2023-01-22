namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for request messages.
/// </summary>
/// <typeparam name="TResponse">The response message type associated with the request</typeparam>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IRequestMessage<out TResponse> : IMessage
{
}

/// <summary>
/// Marker interface for request messages.
/// </summary>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IRequestMessage : IRequestMessage<VoidResponseMessage>, IMessage
{
}
