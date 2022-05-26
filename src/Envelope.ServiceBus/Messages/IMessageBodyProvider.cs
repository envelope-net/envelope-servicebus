using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Messages;

public interface IMessageBodyProvider
{
	Task<IResult<Guid>> SaveToStorageAsync<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage;

	Task<IResult<Guid, Guid>> SaveReplyToStorageAsync<TResponse>(Guid messageId, TResponse? response, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);

	Task<IResult<TMessage?, Guid>> LoadFromStorageAsync<TMessage>(IMessageMetadata messageMetadata, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage;
}
