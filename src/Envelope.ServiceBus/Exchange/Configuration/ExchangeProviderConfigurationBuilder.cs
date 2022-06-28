using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Exchange.Internal;
using Envelope.ServiceBus.Exchange.Routing.Configuration;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Internal;

namespace Envelope.ServiceBus.Exchange.Configuration;

public interface IExchangeProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IExchangeProviderConfigurationBuilder<TBuilder, TObject>
	where TObject : IExchangeProviderConfiguration
{
	TBuilder Object(TObject exchangeProviderConfiguration);

	TObject Internal();

	TObject Build(bool finalize = false);

	TBuilder FaultQueue(Func<IServiceProvider, IFaultQueue> faultQueue, bool force = true);

	TBuilder RegisterInMemoryExchange<TMessage>(Action<ExchangeRouterBuilder>? configureBindings = null, bool force = true)
		where TMessage : class, IMessage;

	TBuilder RegisterExchange<TMessage>(Action<ExchangeConfigurationBuilder<TMessage>> configure, bool force = true)
		where TMessage : class, IMessage;

	TBuilder RegisterExchange<TMessage>(string exchangeName, Func<IServiceProvider, IExchange<TMessage>> exchange, bool force = true)
		where TMessage : class, IMessage;
}

public abstract class ExchangeProviderConfigurationBuilderBase<TBuilder, TObject> : IExchangeProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : ExchangeProviderConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IExchangeProviderConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _exchangeProviderConfiguration;

	protected ExchangeProviderConfigurationBuilderBase(TObject exchangeProviderConfiguration)
	{
		_exchangeProviderConfiguration = exchangeProviderConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject exchangeProviderConfiguration)
	{
		_exchangeProviderConfiguration = exchangeProviderConfiguration;
		return _builder;
	}

	public TObject Internal()
		=> _exchangeProviderConfiguration;

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _exchangeProviderConfiguration.Validate(nameof(IExchangeProviderConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _exchangeProviderConfiguration;
	}

	public TBuilder FaultQueue(Func<IServiceProvider, IFaultQueue> faultQueue, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _exchangeProviderConfiguration.FaultQueue == null)
			_exchangeProviderConfiguration.FaultQueue = faultQueue;

		return _builder;
	}

	public TBuilder RegisterInMemoryExchange<TMessage>(Action<ExchangeRouterBuilder>? configureBindings = null, bool force = true)
		where TMessage : class, IMessage
		=> RegisterExchange(
			typeof(TMessage).FullName!,
			sp =>
			{
				var exchangeConfiguration = ExchangeConfigurationBuilder<TMessage>
					.GetDefaultBuilder(_exchangeProviderConfiguration.ServiceBusOptions, configureBindings)
					.Build();
				var context = new ExchangeContext<TMessage>(exchangeConfiguration);
				return new Exchange<TMessage>(context);
			},
			force);

	public TBuilder RegisterExchange<TMessage>(Action<ExchangeConfigurationBuilder<TMessage>> configure, bool force = true)
		where TMessage : class, IMessage
		=> configure != null
			? RegisterExchange(
				typeof(TMessage).FullName!,
				sp =>
				{
					var builder = ExchangeConfigurationBuilder<TMessage>
						.GetDefaultBuilder(_exchangeProviderConfiguration.ServiceBusOptions, null);
					configure.Invoke(builder);
					var exchangeConfiguration = builder.Build();
					var context = new ExchangeContext<TMessage>(exchangeConfiguration);
					return new Exchange<TMessage>(context);
				},
				force)
			: throw new ArgumentNullException(nameof(configure));

	public TBuilder RegisterExchange<TMessage>(string exchangeName, Func<IServiceProvider, IExchange<TMessage>> exchange, bool force = true)
		where TMessage : class, IMessage
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force)
			_exchangeProviderConfiguration.Exchanges[exchangeName] = exchange;
		else
			_exchangeProviderConfiguration.Exchanges.TryAdd(exchangeName, exchange);

		return _builder;
	}
}

public class ExchangeProviderConfigurationBuilder : ExchangeProviderConfigurationBuilderBase<ExchangeProviderConfigurationBuilder, IExchangeProviderConfiguration>
{
	public ExchangeProviderConfigurationBuilder(IServiceBusOptions serviceBusOptions)
		: base(new ExchangeProviderConfiguration(serviceBusOptions))
	{
	}

	private ExchangeProviderConfigurationBuilder(ExchangeProviderConfiguration exchangeProviderConfiguration)
		: base(exchangeProviderConfiguration)
	{
	}

	public static implicit operator ExchangeProviderConfiguration?(ExchangeProviderConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._exchangeProviderConfiguration as ExchangeProviderConfiguration;
	}

	public static implicit operator ExchangeProviderConfigurationBuilder?(ExchangeProviderConfiguration exchangeProviderConfiguration)
	{
		if (exchangeProviderConfiguration == null)
			return null;

		return new ExchangeProviderConfigurationBuilder(exchangeProviderConfiguration);
	}

	internal static ExchangeProviderConfigurationBuilder GetDefaultBuilder(
		IServiceBusOptions serviceBusOptions,
		Action<ExchangeConfigurationBuilder<OrchestrationEvent>>? orchestrationExchange = null,
		Func<IServiceProvider, IFaultQueue>? orchestrationEventsFaultQueue = null)
		=> orchestrationExchange != null
			? new ExchangeProviderConfigurationBuilder(serviceBusOptions)
				.RegisterExchange(orchestrationExchange)
				.FaultQueue(orchestrationEventsFaultQueue ?? (sp => new DroppingFaultQueue()))
			: new ExchangeProviderConfigurationBuilder(serviceBusOptions)
				.RegisterInMemoryExchange<OrchestrationEvent>()
				.FaultQueue(sp => new DroppingFaultQueue());
}
