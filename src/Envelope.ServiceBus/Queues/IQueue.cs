using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Queues;

public interface IQueue : IDisposable
{
	int Count { get; }

	int? MaxSize { get; set; }

	Task<IResult> EnqueueAsync(List<IMessageMetadata> messagesMetadata, ITraceInfo traceInfo);

	Task<IResult> TryRemoveAsync(IMessageMetadata messageMetadata, ITraceInfo traceInfo);

	/// <inheritdoc/>
	Task<IResult<IMessageMetadata?>> TryPeekAsync(ITraceInfo traceInfo);
}
