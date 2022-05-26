namespace Envelope.ServiceBus.Messages;

/// <summary>
/// Marker interface for commands.
/// </summary>
/// <typeparam name="TResponse">The response message type associated with the command</typeparam>
public interface ICommand<out TResponse> : IRequestMessage<TResponse>, IMessage
{
}

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface ICommand: ICommand<VoidResponseMessage>, IRequestMessage, IRequestMessage<VoidResponseMessage>, IMessage
{
}
