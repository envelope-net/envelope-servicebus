using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Exchange.Internal;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.ServiceBus.Queues.Internal;

namespace Envelope.ServiceBus.Configuration;

public static class ServiceBusOptionsFactory
{
	public static IServiceBusOptions Create(IServiceProvider serviceProvider, Action<ServiceBusConfigurationBuilder> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = ServiceBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);
		var configuration = builder.Build();

		return Create(serviceProvider, configuration);
	}

	public static IServiceBusOptions Create(IServiceProvider serviceProvider, IServiceBusConfiguration configuration)
	{
		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		var error = configuration.Validate(nameof(ServiceBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		var options = new ServiceBusOptions(serviceProvider)
		{
			ServiceBusMode = configuration.ServiceBusMode!.Value,
			HostInfo = configuration.HostInfo ?? new HostInfo(configuration.ServiceBusName),
			MessageTypeResolver = configuration.MessageTypeResolver(serviceProvider),
			HostLogger = configuration.HostLogger(serviceProvider),
			TransactionManagerFactory = configuration.TransactionManagerFactory(serviceProvider),
			TransactionContextFactory = configuration.TransactionContextFactory,
			MessageHandlerContextType = configuration.MessageHandlerContextType,
			MessageHandlerContextFactory = configuration.MessageHandlerContextFactory,
			HandlerLogger = configuration.HandlerLogger(serviceProvider),
			MessageHandlerResultFactory = new MessageHandlerResultFactory() //MessageHandlerResultFactory(serviceProvider)
		};

		foreach (var handler in configuration.ServiceBusEventHandlers)
			options.ServiceBusLifeCycleEventManager.OnServiceBusEvent += handler;

		var exchangeProviderBuilder =
			ExchangeProviderConfigurationBuilder.GetDefaultBuilder(
				options,
				configuration.OrchestrationExchange,
				configuration.OrchestrationEventsFaultQueue);

		configuration.ExchangeProviderConfiguration?.Invoke(exchangeProviderBuilder);
		var exchangeProviderConfiguration = exchangeProviderBuilder.Build();
		var exchangeProvider = new ExchangeProvider(serviceProvider, exchangeProviderConfiguration, options);

		var queueProviderBuilder =
			QueueProviderConfigurationBuilder.GetDefaultBuilder(
				options,
				configuration.OrchestrationQueue,
				configuration.OrchestrationEventsFaultQueue);

		configuration.QueueProviderConfiguration?.Invoke(queueProviderBuilder);
		var queueProviderConfiguration = queueProviderBuilder.Build();
		var queueProvider = new QueueProvider(serviceProvider, queueProviderConfiguration, options);

		options.ExchangeProvider = exchangeProvider;
		options.QueueProvider = queueProvider;

		error = options.Validate(nameof(ServiceBusOptions));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return options;
	}
}
