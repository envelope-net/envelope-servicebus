using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Exchange;

public interface IMessageBrokerHandler<TMessage>
	where TMessage : class, IMessage
{
	Task<MessageHandlerResult> HandleAsync(
		IExchangeMessage<TMessage> message,
		ExchangeContext<TMessage> _exchangeContext,
		ITransactionController transactionController,
		CancellationToken cancellationToken);
}
