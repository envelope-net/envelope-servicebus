namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for queries.
/// </summary>
/// <typeparam name="TResponse">The response message type associated with the query</typeparam>
public interface IQuery<out TResponse> : IRequestMessage<TResponse>, IMessage
{
}
