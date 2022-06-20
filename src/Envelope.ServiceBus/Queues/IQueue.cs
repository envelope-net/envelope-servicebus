using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queues;

public interface IQueue<T> : IDisposable
	where T : IMessageMetadata
{
	int? MaxSize { get; set; }

	Task<IResult<int>> GetCountAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task<IResult> EnqueueAsync(List<T> messagesMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	Task<IResult<T?>> TryPeekAsync(ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task<IResult> TryRemoveAsync(T messageMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken = default);

	Task<IResult<QueueStatus>> UpdateAsync(T messageMetadata, IMessageMetadataUpdate update, ITraceInfo traceInfo, ITransactionContext localTransactionContext, CancellationToken cancellationToken = default);
}
