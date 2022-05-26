using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Queues;

public interface IQueue : IDisposable
{
	int Count { get; }

	int? MaxSize { get; set; }

	Task<IResult<Guid>> EnqueueAsync(List<IMessageMetadata> messagesMetadata, ITraceInfo<Guid> traceInfo);

	Task<IResult<Guid>> TryRemoveAsync(IMessageMetadata messageMetadata, ITraceInfo<Guid> traceInfo);

	/// <inheritdoc/>
	Task<IResult<IMessageMetadata?, Guid>> TryPeekAsync(ITraceInfo<Guid> traceInfo);
}
