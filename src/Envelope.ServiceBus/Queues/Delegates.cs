using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Queues;

public delegate Task<MessageHandlerResult> HandleMessage<TMessage>(IQueuedMessage<TMessage> message, IMessageHandlerContext context, CancellationToken cancellationToken)
	where TMessage : class, IMessage;
