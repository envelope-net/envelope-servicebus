using Envelope.ServiceBus.MessageHandlers;

namespace Envelope.ServiceBus.Configuration;

public interface IEventHandlerRegistry
{
	MessageHandlerContext? CreateEventHandlerContext(Type eventType, IServiceProvider serviceProvider);
}
