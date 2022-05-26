using Envelope.ServiceBus.MessageHandlers;

namespace Envelope.ServiceBus.Configuration;

public interface IMessageHandlerRegistry
{
	MessageHandlerContext? CreateMessageHandlerContext(Type messageType, IServiceProvider serviceProvider);
}
