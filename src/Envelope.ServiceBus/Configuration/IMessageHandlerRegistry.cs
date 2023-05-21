using Envelope.ServiceBus.MessageHandlers;

namespace Envelope.ServiceBus.Configuration;

public interface IMessageHandlerRegistry
{
	IMessageHandlerContext? CreateMessageHandlerContext(Type messageType, IServiceProvider serviceProvider);
}
