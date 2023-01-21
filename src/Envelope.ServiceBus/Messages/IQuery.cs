namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for queries.
/// </summary>
/// <typeparam name="TResponse">The response message type associated with the query</typeparam>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IQuery<out TResponse> : IRequestMessage<TResponse>, IMessage
{
}
