using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Exchange.Internal;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Exchange.Routing.Configuration;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Internal;

namespace Envelope.ServiceBus.Exchange.Configuration;

public interface IExchangeConfigurationBuilder<TBuilder, TObject, TMessage>
	where TBuilder : IExchangeConfigurationBuilder<TBuilder, TObject, TMessage>
	where TObject : IExchangeConfiguration<TMessage>
	where TMessage : class, IMessage
{
	TBuilder Object(TObject exchangeConfiguration);

	TObject Build(bool finalize = false);

	TBuilder ExchangeName(string exchangeName, bool force = true);

	TBuilder QueueType(QueueType queueType);

	TBuilder StartDelay(TimeSpan? startDelay);

	TBuilder FetchInterval(TimeSpan fetchInterval);

	TBuilder MaxSize(int? maxSize, bool force = true);

	TBuilder ExchangeMessageFactory(Func<IServiceProvider, IExchangeMessageFactory<TMessage>> exchangeMessageFactory, bool force = true);

	TBuilder MessageBrokerHandler(Func<IServiceProvider, IMessageBrokerHandler<TMessage>> messageBrokerHandler, bool force = true);

	TBuilder FIFOQueue(Func<IServiceProvider, int?, IQueue<IExchangeMessage<TMessage>>> fifoQueue, bool force = true);

	TBuilder DelayableQueue(Func<IServiceProvider, int?, IQueue<IExchangeMessage<TMessage>>> delayableQueue, bool force = true);

	TBuilder MessageBodyProvider(Func<IServiceProvider, IMessageBodyProvider> messageBodyProvider, bool force = true);

	TBuilder Router(Func<IServiceProvider, IExhcangeRouter> router, bool force = true);

	TBuilder ErrorHandling(Func<IServiceProvider, IErrorHandlingController>? errorHandling, bool force = true);
}

public abstract class ExchangeConfigurationBuilderBase<TBuilder, TObject, TMessage> : IExchangeConfigurationBuilder<TBuilder, TObject, TMessage>
	where TBuilder : ExchangeConfigurationBuilderBase<TBuilder, TObject, TMessage>
	where TObject : IExchangeConfiguration<TMessage>
	where TMessage : class, IMessage
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _exchangeConfiguration;

	protected ExchangeConfigurationBuilderBase(TObject exchangeConfiguration)
	{
		_exchangeConfiguration = exchangeConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject exchangeConfiguration)
	{
		_exchangeConfiguration = exchangeConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _exchangeConfiguration.Validate(nameof(IExchangeConfiguration<TMessage>));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _exchangeConfiguration;
	}

	public TBuilder ExchangeName(string exchangeName, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_exchangeConfiguration.ExchangeName))
			_exchangeConfiguration.ExchangeName = exchangeName;

		return _builder;
	}

	public TBuilder QueueType(QueueType queueType)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_exchangeConfiguration.QueueType = queueType;
		return _builder;
	}

	public TBuilder StartDelay(TimeSpan? startDelay)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_exchangeConfiguration.StartDelay = startDelay;
		return _builder;
	}

	public TBuilder FetchInterval(TimeSpan fetchInterval)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_exchangeConfiguration.FetchInterval = fetchInterval;
		return _builder;
	}

	public TBuilder MaxSize(int? maxSize, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_exchangeConfiguration.MaxSize.HasValue)
			_exchangeConfiguration.MaxSize = maxSize;

		return _builder;
	}

	public TBuilder ExchangeMessageFactory(Func<IServiceProvider, IExchangeMessageFactory<TMessage>> exchangeMessageFactory, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.ExchangeMessageFactory == null)
			_exchangeConfiguration.ExchangeMessageFactory = exchangeMessageFactory;

		return _builder;
	}

	public TBuilder MessageBrokerHandler(Func<IServiceProvider, IMessageBrokerHandler<TMessage>> messageBrokerHandler, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.MessageBrokerHandler == null)
			_exchangeConfiguration.MessageBrokerHandler = messageBrokerHandler;

		return _builder;
	}

	public TBuilder FIFOQueue(Func<IServiceProvider, int?, IQueue<IExchangeMessage<TMessage>>> fifoQueue, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.FIFOQueue == null)
			_exchangeConfiguration.FIFOQueue = fifoQueue;

		return _builder;
	}

	public TBuilder DelayableQueue(Func<IServiceProvider, int?, IQueue<IExchangeMessage<TMessage>>> delayableQueue, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.DelayableQueue == null)
			_exchangeConfiguration.DelayableQueue = delayableQueue;

		return _builder;
	}

	public TBuilder MessageBodyProvider(Func<IServiceProvider, IMessageBodyProvider> messageBodyProvider, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.MessageBodyProvider == null)
			_exchangeConfiguration.MessageBodyProvider = messageBodyProvider;

		return _builder;
	}

	public TBuilder Router(Func<IServiceProvider, IExhcangeRouter> router, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.Router == null)
			_exchangeConfiguration.Router = router;

		return _builder;
	}

	public TBuilder ErrorHandling(Func<IServiceProvider, IErrorHandlingController>? errorHandling, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeConfiguration.ErrorHandling == null)
			_exchangeConfiguration.ErrorHandling = errorHandling;

		return _builder;
	}
}

public class ExchangeConfigurationBuilder<TMessage> : ExchangeConfigurationBuilderBase<ExchangeConfigurationBuilder<TMessage>, IExchangeConfiguration<TMessage>, TMessage>
		where TMessage : class, IMessage
{
	public ExchangeConfigurationBuilder(IServiceBusOptions serviceBusOptions)
		: base(new ExchangeConfiguration<TMessage>(serviceBusOptions))
	{
	}

	private ExchangeConfigurationBuilder(ExchangeConfiguration<TMessage> exchangeConfiguration)
		: base(exchangeConfiguration)
	{
	}

	public static implicit operator ExchangeConfiguration<TMessage>?(ExchangeConfigurationBuilder<TMessage> builder)
	{
		if (builder == null)
			return null;

		return builder._exchangeConfiguration as ExchangeConfiguration<TMessage>;
	}

	public static implicit operator ExchangeConfigurationBuilder<TMessage>?(ExchangeConfiguration<TMessage> exchangeConfiguration)
	{
		if (exchangeConfiguration == null)
			return null;

		return new ExchangeConfigurationBuilder<TMessage>(exchangeConfiguration);
	}

	public static ExchangeConfigurationBuilder<TMessage> GetDefaultBuilder(
		IServiceBusOptions serviceBusOptions,
		Action<ExchangeRouterBuilder>? configureBindings = null)
	{
		var bindingsBuilder = ExchangeRouterBuilder.GetDefaultBuilder<TMessage>();
		configureBindings?.Invoke(bindingsBuilder);
		var exchangeRouter = bindingsBuilder.Build();

		var result =
			new ExchangeConfigurationBuilder<TMessage>(serviceBusOptions)
				.ExchangeName(typeof(TMessage).FullName!)
				.QueueType(Queues.QueueType.Sequential_Delayable)
				//.StartDelay(null)
				//.MaxSize(null)
				//.ErrorHandling(sp => null)
				.ExchangeMessageFactory(sp => new ExchangeMessageFactory<TMessage>())
				.MessageBrokerHandler(sp => new MessageBrokerHandler<TMessage>())
				.FIFOQueue((sp, maxSize) => new InMemoryFIFOQueue<IExchangeMessage<TMessage>>(maxSize))
				.DelayableQueue((sp, maxSize) => new InMemoryDelayableQueue<IExchangeMessage<TMessage>>(maxSize))
				.FetchInterval(TimeSpan.FromMilliseconds(1))
				.MessageBodyProvider(sp => new InMemoryMessageBodyProvider(TimeSpan.FromMinutes(1)))
				.Router(sp => exchangeRouter);

		return result;
	}
}
