using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues.Configuration;

namespace Envelope.ServiceBus.Queues;

public class MessageQueueContext<TMessage>
		where TMessage : class, IMessage
{
	private readonly IMessageQueueConfiguration<TMessage> _messageQueueConfiguration;
	private readonly Lazy<IQueue<IQueuedMessage<TMessage>>> _fifoQueue;
	private readonly Lazy<IQueue<IQueuedMessage<TMessage>>> _delayableQueue;
	private readonly Lazy<IMessageBodyProvider> _messageBodyProvider;
	private readonly Lazy<HandleMessage<TMessage>>? _messageHandler;
	private readonly Lazy<IErrorHandlingController>? _errorHandling;

	public IServiceProvider ServiceProvider { get; }

	public IServiceBusOptions ServiceBusOptions { get; }

	public string QueueName { get; }

	public QueueType QueueType { get; }

	public bool IsPull { get; }

	public TimeSpan? StartDelay { get; }

	public TimeSpan FetchInterval { get; }

	public int? MaxSize { get; }

	public TimeSpan? DefaultProcessingTimeout { get; }

	public IQueue<IQueuedMessage<TMessage>> FIFOQueue => _fifoQueue.Value;

	public IQueue<IQueuedMessage<TMessage>> DelayableQueue => _delayableQueue.Value;

	/// <inheritdoc/>
	public IMessageBodyProvider MessageBodyProvider => _messageBodyProvider.Value;

	/// <inheritdoc/>
	public HandleMessage<TMessage>? MessageHandler => _messageHandler?.Value;

	public IErrorHandlingController? ErrorHandling => _errorHandling?.Value;

	public MessageQueueContext(IMessageQueueConfiguration<TMessage> messageQueueConfiguration, IServiceProvider serviceProvider)
	{
		_messageQueueConfiguration = messageQueueConfiguration ?? throw new ArgumentNullException(nameof(messageQueueConfiguration));

		ServiceBusOptions = _messageQueueConfiguration.ServiceBusOptions;
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		QueueName = _messageQueueConfiguration.QueueName;
		QueueType = _messageQueueConfiguration.QueueType;
		IsPull = _messageQueueConfiguration.IsPull;
		StartDelay = _messageQueueConfiguration.StartDelay;
		FetchInterval = _messageQueueConfiguration.FetchInterval;
		MaxSize = _messageQueueConfiguration.MaxSize;
		DefaultProcessingTimeout = _messageQueueConfiguration.DefaultProcessingTimeout;

		_fifoQueue = new(() =>
		{
			var fifoQueue = _messageQueueConfiguration.FIFOQueue(ServiceProvider, _messageQueueConfiguration.MaxSize);
			if (fifoQueue == null)
				throw new InvalidOperationException(nameof(fifoQueue));

			return fifoQueue;
		});
		_delayableQueue = new(() =>
		{
			var delayableQueue = _messageQueueConfiguration.DelayableQueue(ServiceProvider, _messageQueueConfiguration.MaxSize);
			if (delayableQueue == null)
				throw new InvalidOperationException(nameof(delayableQueue));

			return delayableQueue;
		});
		_messageBodyProvider = new(() =>
		{
			var messageBodyProvider = _messageQueueConfiguration.MessageBodyProvider(ServiceProvider);
			if (messageBodyProvider == null)
				throw new InvalidOperationException(nameof(messageBodyProvider));

			return messageBodyProvider;
		});
		_messageHandler = _messageQueueConfiguration.MessageHandler != null
			? new(() =>
			{
				var messageHandler = _messageQueueConfiguration.MessageHandler.Invoke(ServiceProvider, ServiceBusOptions);
				if (messageHandler == null)
					throw new InvalidOperationException(nameof(messageHandler));

				return messageHandler;
			})
			: null;
		_errorHandling = _messageQueueConfiguration.ErrorHandling != null
			? new(() =>
			{
				var errorHandling = _messageQueueConfiguration.ErrorHandling.Invoke(ServiceProvider);
				if (errorHandling == null)
					throw new InvalidOperationException(nameof(errorHandling));

				return errorHandling;
			})
			: null;
	}
}
