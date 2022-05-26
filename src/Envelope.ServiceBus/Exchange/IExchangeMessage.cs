using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Exchange;

public interface IExchangeMessage<TMessage> : IMessageMetadata, ISavedMessage<TMessage>, IMessageInfo
	where TMessage : class, IMessage
{
	string ExchangeName { get; }

	/// <summary>
	/// If true, the message sending was called without waiting for any response. The response will be delivered
	/// in asynchronous way, the CorrespondingMessageId would be writen to reply queue and the publisher will be notified,
	/// when the reply arives. If false, the response message will be returned synchronously to caller in timeout duration.
	/// </summary>
	bool IsAsynchronousInvocation { get; }

	/// <summary>
	/// Routing ... alternatively the target queue name
	/// </summary>
	string? RoutingKey { get; }

	string TargetQueueName { get; }

	bool DisableFaultQueue { get; set; }
}
