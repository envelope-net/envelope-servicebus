using Envelope.Policy;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using System.Text;

namespace Envelope.ServiceBus.Queues;

public interface IQueueEnqueueContext
{
	string SourceExchangeName { get; }

	/// <summary>
	/// MessageId
	/// </summary>
	Guid MessageId { get; }

	/// <summary>
	/// Parent messageId, when the parent message was cloned to multiple subscribers as an individual message
	/// </summary>
	Guid? ParentMessageId { get; }

	/// <summary>
	/// Publisher identifier = HostIdentifier + TraceInfo.TraceFrame
	/// </summary>
	string PublisherId { get; }

	/// <summary>
	/// Publisher source trace frame
	/// </summary>
	ITraceInfo<Guid> TraceInfo { get; }

	/// <summary>
	/// If true, the message will never be saved to storage
	/// </summary>
	bool DisabledMessagePersistence { get; }

	/// <summary>
	/// Id of the original request that launched the session. Used for tracing messages
	/// </summary>
	Guid? IdSession { get; }

	string? ContentType { get; }

	Encoding? ContentEncoding { get; }

	string? RoutingKey { get; }

	IErrorHandlingController? ErrorHandling { get; }

	IMessageHeaders? Headers { get; }

	/// <summary>
	/// If true, the message sending was called without waiting for any response. The response will be delivered
	/// in asynchronous way, the CorrespondingMessageId would be writen to reply queue and the publisher will be notified,
	/// when the reply arives. If false, the response message will be returned synchronously to caller in timeout duration.
	/// </summary>
	bool IsAsynchronousInvocation { get; }

	/// <summary>
	/// The timespan after which the Send request will be cancelled if no response arrives.
	/// </summary>
	TimeSpan? Timeout { get; }

	bool IsCompressedContent { get; }

	bool IsEncryptedContent { get; }

	int Priority { get; }

	bool DisableFaultQueue { get; }
}
