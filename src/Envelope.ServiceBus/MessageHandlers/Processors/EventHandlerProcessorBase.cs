namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class EventHandlerProcessorBase
{
	protected abstract IEnumerable<IEventHandler> CreateHandlers(IServiceProvider serviceProvider);
}
