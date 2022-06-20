using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

public interface IMessageQueueConfiguration<TMessage> : IValidable
		where TMessage : class, IMessage
{
	IServiceBusOptions ServiceBusOptions { get; }

	/// <summary>
	/// Unique queue name on the host
	/// </summary>
	string QueueName { get; set; }

	QueueType QueueType { get; set; }

	/// <summary>
	/// If true, messages are waiting until the subscribers pick them up,
	/// else the queue push the messages to subscribers
	/// </summary>
	bool IsPull { get; set; }

	/// <summary>
	/// Queue fetching start delay timeout
	/// </summary>
	TimeSpan? StartDelay { get; set; }

	/// <summary>
	/// Fetch messages interval
	/// </summary>
	TimeSpan FetchInterval { get; set; }

	/// <summary>
	/// Queue max size
	/// </summary>
	int? MaxSize { get; set; }

	/// <summary>
	/// The timespan after which the message processing will be cancelled.
	/// </summary>
	TimeSpan? DefaultProcessingTimeout { get; set; }

	Func<IServiceProvider, int?, IQueue<IQueuedMessage<TMessage>>> FIFOQueue { get; set; }

	Func<IServiceProvider, int?, IQueue<IQueuedMessage<TMessage>>> DelayableQueue { get; set; }

	/// <summary>
	/// <see cref="IMessageBodyProvider"/> is responsible for message body saving
	/// and loading, serialization, encryption and compression
	/// </summary>
	Func<IServiceProvider, IMessageBodyProvider> MessageBodyProvider { get; set; }

	Func<IServiceProvider, IServiceBusOptions, HandleMessage<TMessage>>? MessageHandler { get; set; }

	Func<IServiceProvider, IErrorHandlingController>? ErrorHandling { get; set; }
}
