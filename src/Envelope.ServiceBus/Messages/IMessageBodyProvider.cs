using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Messages;

public interface IMessageBodyProvider
{
	Task<IResult> SaveToStorageAsync<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage;

	Task<IResult<Guid>> SaveReplyToStorageAsync<TResponse>(Guid messageId, TResponse? response, ITraceInfo traceInfo, CancellationToken cancellationToken);

	Task<IResult<TMessage?>> LoadFromStorageAsync<TMessage>(IMessageMetadata messageMetadata, ITraceInfo traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage;
}
