using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Exchange;

public interface IMessageBrokerHandler<TMessage>
	where TMessage : class, IMessage
{
	Task<MessageHandlerResult> HandleAsync(
		IExchangeMessage<TMessage> message,
		ExchangeContext<TMessage> _exchangeContext,
		CancellationToken cancellationToken);
}
