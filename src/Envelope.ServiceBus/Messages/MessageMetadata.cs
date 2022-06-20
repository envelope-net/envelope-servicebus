using Envelope.ServiceBus.ErrorHandling;
using Envelope.Trace;
using System.Text;

namespace Envelope.ServiceBus.Messages;

public class MessageMetadata<TMessage> : IMessageMetadata, ISavedMessage<TMessage>
	where TMessage : class, IMessage
{
	/// <inheritdoc/>
	public Guid MessageId { get; set; }

	/// <inheritdoc/>
	public virtual bool Processed { get => true; set => throw new NotSupportedException($"Set {nameof(Processed)} is not supported in {nameof(MessageMetadata<TMessage>)}"); }

	/// <inheritdoc/>
	public Guid? ParentMessageId { get; set; }

	/// <inheritdoc/>
	public DateTime PublishingTimeUtc { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	/// <inheritdoc/>
	public string PublisherId { get; set; }

	/// <inheritdoc/>
	public ITraceInfo TraceInfo { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	/// <inheritdoc/>
	public TimeSpan? Timeout { get; set; }

	/// <inheritdoc/>
	public DateTime? TimeToLiveUtc => Timeout.HasValue
		? PublishingTimeUtc.Add(Timeout.Value)
		: null;

	public Guid? IdSession { get; set; }

	/// <inheritdoc/>
	public string? ContentType { get; set; }

	/// <inheritdoc/>
	public Encoding? ContentEncoding { get; set; }

	/// <inheritdoc/>
	public bool IsCompressedContent { get; set; }

	/// <inheritdoc/>
	public bool IsEncryptedContent { get; set; }

	/// <inheritdoc/>
	public bool ContainsContent { get; set; }

	public bool HasSelfContent { get; set; }

	/// <inheritdoc/>
	public int Priority { get; set; }

	/// <inheritdoc/>
	public IEnumerable<KeyValuePair<string, object>>? Headers { get; set; }

	/// <inheritdoc/>
	public bool DisabledMessagePersistence { get; set; }

	/// <inheritdoc/>
	public MessageStatus MessageStatus { get; set; }

	/// <inheritdoc/>
	public int RetryCount { get; set; }

	/// <inheritdoc/>
	public IErrorHandlingController? ErrorHandling { get; set; }

	/// <inheritdoc/>
	public DateTime? DelayedToUtc { get; set; }

	/// <inheritdoc/>
	public TMessage? Message { get; set; }

	public void SetMessage(object message)
	{
		if (message == null)
			Message = null;

		if (message is not TMessage msg)
			throw new InvalidOperationException($"{nameof(message)} must be type of {typeof(TMessage).FullName}");

		Message = msg;
	}

	internal void Update(bool processed, MessageStatus status, int retryCount, DateTime? delayedToUtc)
	{
		Processed = processed;
		MessageStatus = status;
		RetryCount = retryCount;
		DelayedToUtc = delayedToUtc;
	}

	void IMessageMetadata.Update(bool processed, MessageStatus status, int retryCount, DateTime? delayedToUtc)
		=> Update(processed, status, retryCount, delayedToUtc);
}
