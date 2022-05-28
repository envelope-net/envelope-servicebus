using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Exchange;

public interface IExchangeProvider
{
	/// <summary>
	/// Prepresents the falt queue, e.g. when no exchange with specific name was created,
	/// the message will be inserted to fault queue.
	/// </summary>
	IFaultQueue FaultQueue { get; }

	/// <summary>
	/// Get's the exchange for the specific exchange name and message type
	/// </summary>
	IExchange<TMessage>? GetExchange<TMessage>(string exchangeName)
		where TMessage : class, IMessage;

	IResult<IExchangeEnqueueContext> CreateExchangeEnqueueContext(ITraceInfo traceInfo, IMessageOptions options, ExchangeType exchangeType);

	IFaultQueueContext CreateFaultQueueContext(ITraceInfo traceInfo, IMessageOptions options);
}
