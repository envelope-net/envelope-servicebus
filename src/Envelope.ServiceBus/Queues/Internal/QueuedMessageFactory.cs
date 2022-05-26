using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues.Configuration;

namespace Envelope.ServiceBus.Queues.Internal;

internal class QueuedMessageFactory<TMessage>
	where TMessage : class, IMessage
{
	public static IQueuedMessage<TMessage> CreateQueuedMessage(TMessage? message, IQueueEnqueueContext context, IMessageQueueConfiguration<TMessage> config)
	{
		var nowUtc = DateTime.UtcNow;
		var exchangeMessage = new QueuedMessage<TMessage>
		{
			SourceExchangeName = "",
			QueueName = config.QueueName,
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
