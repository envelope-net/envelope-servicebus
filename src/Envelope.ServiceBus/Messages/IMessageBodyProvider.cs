using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Messages;

public interface IMessageBodyProvider
{
	bool AllowMessagePersistence(bool disabledMessagePersistence, IMessageMetadata message);

	bool AllowAnyMessagePersistence(bool disabledMessagePersistence, IEnumerable<IMessageMetadata> message);

	Task<IResult> SaveToStorageAsync<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
		where TMessage : class, IMessage;

	Task<IResult<Guid>> SaveReplyToStorageAsync<TResponse>(Guid messageId, TResponse? response, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken);

	Task<IResult<TMessage?>> LoadFromStorageAsync<TMessage>(IMessageMetadata messageMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
		where TMessage : class, IMessage;

	IResult SaveToStorage<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo traceInfo, ITransactionContext transactionContext)
		where TMessage : class, IMessage;

	IResult<Guid> SaveReplyToStorage<TResponse>(Guid messageId, TResponse? response, ITraceInfo traceInfo, ITransactionContext transactionContext);
}
