using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Trace;

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
	Task<IResult<List<Guid>, Guid>> EnqueueAsync(TMessage? message, IExchangeEnqueueContext context, CancellationToken cancellationToken);

	Task<IResult<IExchangeMessage<TMessage>?, Guid>> TryPeekAsync(ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);

	Task<IResult<Guid>> TryRemoveAsync(IExchangeMessage<TMessage> message, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);
}
