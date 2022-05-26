using Envelope.ServiceBus.ErrorHandling;
using Envelope.Trace;
using System.Text;

namespace Envelope.ServiceBus.Messages;

public interface IMessageMetadata : IMessageInfo
{
	/// <summary>
	/// Parent messageId, when the parent message was cloned to multiple subscribers as an individual message
	/// </summary>
	Guid? ParentMessageId { get; }

	/// <summary>
	/// The time the message was published.
	/// </summary>
	DateTime PublishingTimeUtc { get; }

	/// <summary>
	/// Publisher identifier = HostIdentifier + TraceInfo.TraceFrame
	/// </summary>
	string PublisherId { get; }

	/// <summary>
	/// Publisher source trace frame
	/// </summary>
	ITraceInfo<Guid> TraceInfo { get; }

	/// <summary>
	/// The time after which the message will be deprecated
	/// </summary>
	TimeSpan? Timeout { get; }

	/// <summary>
	/// The time after which the message will be deprecated. Based on <see cref="PublishingTimeUtc"/> and <see cref="Timeout"/>
	/// </summary>
	DateTime? TimeToLiveUtc { get; }

	/// <summary>
	/// Id of the original request that launched the session. Used for tracing messages
	/// </summary>
	Guid? IdSession { get; }

	string? ContentType { get; }

	Encoding? ContentEncoding { get; }

	bool IsCompressedContent { get; }

	bool IsEncryptedContent { get; }

	bool ContainsContent { get; }

	int Priority { get; }

	IEnumerable<KeyValuePair<string, object>>? Headers { get; }

	bool DisabledMessagePersistence { get; }

	MessageStatus MessageStatus { get; }

	int RetryCount { get; }

	IErrorHandlingController? ErrorHandling { get; }

	DateTime? DelayedToUtc { get; }
}
