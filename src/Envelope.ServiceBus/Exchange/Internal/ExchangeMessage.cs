using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;

namespace Envelope.ServiceBus.Exchange;

internal class ExchangeMessage<TMessage> : MessageMetadata<TMessage>, IExchangeMessage<TMessage>, IMessageMetadata, ISavedMessage<TMessage>, IMessageInfo
	where TMessage : class, IMessage
{
	/// <inheritdoc/>
	public override bool Processed { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	/// <inheritdoc/>
	public string ExchangeName { get; set; }

	/// <inheritdoc/>
	public string TargetQueueName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	/// <inheritdoc/>
	public bool IsAsynchronousInvocation { get; set; }

	/// <inheritdoc/>
	public string? RoutingKey { get; set; }

	public bool DisableFaultQueue { get; set; }
}
