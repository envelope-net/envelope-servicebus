using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Exchange;

public interface IExchange : IQueueInfo, IDisposable, IAsyncDisposable
{
	ExchangeType ExchangeType { get; }
}

public interface IExchange<TMessage> : IExchange, IQueueInfo, IDisposable, IAsyncDisposable
	where TMessage : class, IMessage
{
	/// <summary>
	/// Enqueue the new message
	/// </summary>
	Task<IResult<List<Guid>>> EnqueueAsync(TMessage? message, IExchangeEnqueueContext context, ITransactionContext transactionContext, CancellationToken cancellationToken);

	Task<IResult<IExchangeMessage<TMessage>?>> TryPeekAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken);

	Task<IResult> TryRemoveAsync(IExchangeMessage<TMessage> message, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken);

	internal Task OnMessageAsync(ITraceInfo traceInfo, CancellationToken cancellationToken);
}
