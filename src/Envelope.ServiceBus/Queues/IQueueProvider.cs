﻿using Envelope.ServiceBus.Messages;
using Envelope.Trace;

namespace Envelope.ServiceBus.Queues;

public interface IQueueProvider
{
	/// <summary>
	/// Prepresents the falt queue, e.g. when no queue with specific name was created,
	/// the message will be inserted to fault queue.
	/// </summary>
	IFaultQueue FaultQueue { get; }

	/// <summary>
	/// Get's the queue for the specific queue name and message type
	/// </summary>
	IMessageQueue<TMessage>? GetQueue<TMessage>(string queueName)
		where TMessage : class, IMessage;

	public IQueueEnqueueContext CreateQueueEnqueueContext<TMessage>(ITraceInfo<Guid> traceInfo, Exchange.IExchangeMessage<TMessage> exchangeMessage)
		where TMessage : class, IMessage;

	public IFaultQueueContext CreateFaultQueueContext<TMessage>(ITraceInfo<Guid> traceInfo, Exchange.IExchangeMessage<TMessage> exchangeMessage)
		where TMessage : class, IMessage;
}
