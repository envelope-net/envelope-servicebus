namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class MessageHandlerProcessorBase
{
	protected abstract IMessageHandler CreateHandler(IServiceProvider serviceProvider);
}
