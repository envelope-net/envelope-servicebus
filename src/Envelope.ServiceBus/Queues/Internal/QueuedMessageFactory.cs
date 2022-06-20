using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Queues.Internal;

internal class QueuedMessageFactory<TMessage>
	where TMessage : class, IMessage
{
	public static IQueuedMessage<TMessage> CreateQueuedMessage(TMessage? message, IQueueEnqueueContext context, MessageQueueContext<TMessage> messageQueueContext)
	{
		var nowUtc = DateTime.UtcNow;
		var exchangeMessage = new QueuedMessage<TMessage>
		{
			SourceExchangeName = "",
			QueueName = messageQueueContext.QueueName,
			MessageId = context.MessageId,
			ParentMessageId = context.ParentMessageId,
			PublishingTimeUtc = nowUtc,
			PublisherId = context.PublisherId,
			Processed = false,
			IsAsynchronousInvocation = context.IsAsynchronousInvocation,
			TraceInfo = context.TraceInfo,
			Timeout = context.Timeout,
			IdSession = context.IdSession,
			ContentType = context.ContentType,
			ContentEncoding = context.ContentEncoding,
			IsCompressedContent = context.IsCompressedContent,
			IsEncryptedContent = context.IsEncryptedContent,
			RoutingKey = context.RoutingKey,
			ContainsContent = message != null,
			HasSelfContent = true,
			Priority = context.Priority,
			ErrorHandling = context.ErrorHandling,
			Headers = context.Headers?.GetAll(),
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null,
			Message = message,
			DisabledMessagePersistence = context.DisabledMessagePersistence,
			DisableFaultQueue = context.DisableFaultQueue
		};

		return exchangeMessage;
	}
}
