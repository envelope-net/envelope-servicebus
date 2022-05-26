using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;

namespace Envelope.ServiceBus.Exchange.Configuration;

public class ExchangeContext<TMessage>
		where TMessage : class, IMessage
{
	private readonly IExchangeConfiguration<TMessage> _exchangeConfiguration;
	private readonly Lazy<IExchangeMessageFactory<TMessage>> _exchangeMessageFactory;
	private readonly Lazy<IMessageBrokerHandler<TMessage>> _messageBrokerHandler;
	private readonly Lazy<IQueue> _fifoQueue;
	private readonly Lazy<IQueue> _delayableQueue;
	private readonly Lazy<IMessageBodyProvider> _messageBodyProvider;
	private readonly Lazy<IExhcangeRouter> _router;
	private readonly Lazy<IErrorHandlingController>? _errorHandling;

	/// <inheritdoc/>
	public IServiceBusOptions ServiceBusOptions { get; }

	/// <inheritdoc/>
	public string ExchangeName { get; }

	/// <inheritdoc/>
	public QueueType QueueType { get; }

	/// <inheritdoc/>
	public TimeSpan? StartDelay { get; }

	/// <inheritdoc/>
	public TimeSpan FetchInterval { get; }

	/// <inheritdoc/>
	public int? MaxSize { get; }

	public IExchangeMessageFactory<TMessage> ExchangeMessageFactory => _exchangeMessageFactory.Value;

	public IMessageBrokerHandler<TMessage> MessageBrokerHandler => _messageBrokerHandler.Value;

	public IQueue FIFOQueue => _fifoQueue.Value;

	public IQueue DelayableQueue => _delayableQueue.Value;

	/// <inheritdoc/>
	public IMessageBodyProvider MessageBodyProvider => _messageBodyProvider.Value;

	/// <inheritdoc/>
	public IExhcangeRouter Router => _router.Value;

	public IErrorHandlingController? ErrorHandling => _errorHandling?.Value;

	public ExchangeContext(IExchangeConfiguration<TMessage> exchangeConfiguration)
	{
		ServiceBusOptions = exchangeConfiguration.ServiceBusOptions;
		ExchangeName = exchangeConfiguration.ExchangeName;
		QueueType = exchangeConfiguration.QueueType;
		StartDelay = exchangeConfiguration.StartDelay;
		FetchInterval = exchangeConfiguration.FetchInterval;
		MaxSize = exchangeConfiguration.MaxSize;

		_exchangeConfiguration = exchangeConfiguration ?? throw new ArgumentNullException(nameof(exchangeConfiguration));
		_exchangeMessageFactory = new(() =>
		{
			var exchangeMessageFactory = _exchangeConfiguration.ExchangeMessageFactory(ServiceBusOptions.ServiceProvider);
			if (exchangeMessageFactory == null)
				throw new InvalidOperationException(nameof(exchangeMessageFactory));

			return exchangeMessageFactory;
		});
		_messageBrokerHandler = new(() =>
		{
			var messageBrokerHandler = _exchangeConfiguration.MessageBrokerHandler(ServiceBusOptions.ServiceProvider);
			if (messageBrokerHandler == null)
				throw new InvalidOperationException(nameof(messageBrokerHandler));

			return messageBrokerHandler;
		});
		_fifoQueue = new(() =>
		{
			var fifoQueue = _exchangeConfiguration.FIFOQueue(ServiceBusOptions.ServiceProvider);
			if (fifoQueue == null)
				throw new InvalidOperationException(nameof(fifoQueue));

			return fifoQueue;
		});
		_delayableQueue = new(() =>
		{
			var delayableQueue = _exchangeConfiguration.DelayableQueue(ServiceBusOptions.ServiceProvider);
			if (delayableQueue == null)
				throw new InvalidOperationException(nameof(delayableQueue));

			return delayableQueue;
		});
		_messageBodyProvider = new(() =>
		{
			var messageBodyProvider = _exchangeConfiguration.MessageBodyProvider(ServiceBusOptions.ServiceProvider);
			if (messageBodyProvider == null)
				throw new InvalidOperationException(nameof(messageBodyProvider));

			return messageBodyProvider;
		});
		_router = new(() =>
		{
			var router = _exchangeConfiguration.Router(ServiceBusOptions.ServiceProvider);
			if (router == null)
				throw new InvalidOperationException(nameof(router));

			router.Validate(nameof(Router), null, null);

			return router;
		});
		_errorHandling = _exchangeConfiguration.ErrorHandling != null
			? new(() =>
			{
				var errorHandling = _exchangeConfiguration.ErrorHandling.Invoke(ServiceBusOptions.ServiceProvider);
				if (errorHandling == null)
					throw new InvalidOperationException(nameof(errorHandling));

				return errorHandling;
			})
			: null;
	}
}
