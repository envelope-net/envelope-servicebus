namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for commands.
/// </summary>
/// <typeparam name="TResponse">The response message type associated with the command</typeparam>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ICommand<out TResponse> : IRequestMessage<TResponse>, IMessage
{
}

/// <summary>
/// Marker interface for commands.
/// </summary>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ICommand: ICommand<VoidResponseMessage>, IRequestMessage, IRequestMessage<VoidResponseMessage>, IMessage
{
}
