using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Exchange;

public interface IExchangeMessageFactory<TMessage>
	where TMessage : class, IMessage
{
	IResult<List<IExchangeMessage<TMessage>>, Guid> CreateExchangeMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> exchangeContext,
		ITraceInfo<Guid> traceInfo);
}
