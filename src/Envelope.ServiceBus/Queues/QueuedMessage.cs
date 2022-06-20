using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Queues;

public class QueuedMessage<TMessage> : MessageMetadata<TMessage>, IQueuedMessage<TMessage>, IMessageMetadata, ISavedMessage<TMessage>, IMessageInfo
	where TMessage : class, IMessage
{
	/// <inheritdoc/>
	public override bool Processed { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	/// <inheritdoc/>
	public string SourceExchangeName { get; set; }

	/// <inheritdoc/>
	public string QueueName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	/// <inheritdoc/>
	public bool IsAsynchronousInvocation { get; set; }

	/// <inheritdoc/>
	public string? RoutingKey { get; set; }

	public bool DisableFaultQueue { get; set; }
}
