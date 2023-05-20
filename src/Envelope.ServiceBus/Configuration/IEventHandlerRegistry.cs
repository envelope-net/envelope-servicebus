using Envelope.ServiceBus.MessageHandlers;

namespace Envelope.ServiceBus.Configuration;

public interface IEventHandlerRegistry
{
	IMessageHandlerContext? CreateEventHandlerContext(Type eventType, IServiceProvider serviceProvider);
}
