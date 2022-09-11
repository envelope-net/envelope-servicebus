using Envelope.Exceptions;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.Configuration;

public interface IServiceBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IServiceBusConfigurationBuilder<TBuilder, TObject>
	where TObject : IServiceBusConfiguration
{
	TBuilder Object(TObject serviceBusConfiguration);

	TObject Build(bool finalize = false);

	TBuilder ServiceBusMode(ServiceBusMode serviceBusMode, bool force = true);

	TBuilder HostInfo(IHostInfo hostInfo, bool force = true);

	TBuilder ServiceBusName(string serviceBusName, bool force = true);

	TBuilder MessageTypeResolver(Func<IServiceProvider, IMessageTypeResolver> messageTypeResolver, bool force = true);

	TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = true);

	TBuilder ExchangeProviderConfiguration(Action<ExchangeProviderConfigurationBuilder> exchangeProviderConfiguration, bool force = true);

	TBuilder QueueProviderConfiguration(Action<QueueProviderConfigurationBuilder> queueProviderConfiguration, bool force = true);

	TBuilder MessageHandlerContextFactory<TContext>(Func<IServiceProvider, TContext> messageHandlerContextFactory, bool force = true)
		where TContext : MessageHandlerContext;

	TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = true);

	//TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = true);

	TBuilder AddServiceBusEventHandler(ServiceBusEventHandler serviceBusEventHandler);

	TBuilder OrchestrationEventsFaultQueue(Func<IServiceProvider, IFaultQueue>? orchestrationEventsFaultQueue, bool force = true);

	TBuilder OrchestrationExchange(Action<ExchangeConfigurationBuilder<OrchestrationEvent>>? orchestrationExchange, bool force = true);

	TBuilder OrchestrationQueue(Action<MessageQueueConfigurationBuilder<OrchestrationEvent>>? orchestrationQueue, bool force = true);
}

public abstract class ServiceBusConfigurationBuilderBase<TBuilder, TObject> : IServiceBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : ServiceBusConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IServiceBusConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _serviceBusConfiguration;

	protected ServiceBusConfigurationBuilderBase(TObject serviceBusConfiguration)
	{
		_serviceBusConfiguration = serviceBusConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject serviceBusConfiguration)
	{
		_serviceBusConfiguration = serviceBusConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _serviceBusConfiguration.Validate(nameof(IServiceBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _serviceBusConfiguration;
	}

	public TBuilder ServiceBusMode(ServiceBusMode serviceBusMode, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_serviceBusConfiguration.ServiceBusMode.HasValue)
			_serviceBusConfiguration.ServiceBusMode = serviceBusMode;

		return _builder;
	}

	public TBuilder HostInfo(IHostInfo hostInfo, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.HostInfo == null)
		{
			_serviceBusConfiguration.HostInfo = hostInfo;
			_serviceBusConfiguration.ServiceBusName = hostInfo.HostName;
		}

		return _builder;
	}

	public TBuilder ServiceBusName(string serviceBusName, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_serviceBusConfiguration.ServiceBusName))
			_serviceBusConfiguration.ServiceBusName = serviceBusName;

		return _builder;
	}

	public TBuilder MessageTypeResolver(Func<IServiceProvider, IMessageTypeResolver> messageTypeResolver, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.MessageTypeResolver == null)
			_serviceBusConfiguration.MessageTypeResolver = messageTypeResolver;

		return _builder;
	}

	public TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.HostLogger == null)
			_serviceBusConfiguration.HostLogger = hostLogger;

		return _builder;
	}

	public TBuilder ExchangeProviderConfiguration(Action<ExchangeProviderConfigurationBuilder> exchangeProviderConfiguration, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.ExchangeProviderConfiguration == null)
			_serviceBusConfiguration.ExchangeProviderConfiguration = exchangeProviderConfiguration;

		return _builder;
	}

	public TBuilder QueueProviderConfiguration(Action<QueueProviderConfigurationBuilder> queueProviderConfiguration, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.QueueProviderConfiguration == null)
			_serviceBusConfiguration.QueueProviderConfiguration = queueProviderConfiguration;

		return _builder;
	}

	public TBuilder MessageHandlerContextFactory<TContext>(Func<IServiceProvider, TContext> messageHandlerContextFactory, bool force = true)
		where TContext : MessageHandlerContext
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.MessageHandlerContextFactory == null)
		{
			_serviceBusConfiguration.MessageHandlerContextType = typeof(TContext);
			_serviceBusConfiguration.MessageHandlerContextFactory = messageHandlerContextFactory;
		}

		return _builder;
	}

	public TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.HandlerLogger == null)
			_serviceBusConfiguration.HandlerLogger = handlerLogger;

		return _builder;
	}

	//public TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = true)
	//{
	//	if (_finalized)
	//		throw new ConfigurationException("The builder was finalized");

	//	if (force || _serviceBusConfiguration.MessageHandlerResultFactory == null)
	//		_serviceBusConfiguration.MessageHandlerResultFactory = messageHandlerResultFactory;

	//	return _builder;
	//}

	public TBuilder AddServiceBusEventHandler(ServiceBusEventHandler serviceBusEventHandler)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (serviceBusEventHandler == null)
			throw new ArgumentNullException(nameof(serviceBusEventHandler));

		_serviceBusConfiguration.ServiceBusEventHandlers.Add(serviceBusEventHandler);
		return _builder;
	}

	public TBuilder OrchestrationEventsFaultQueue(Func<IServiceProvider, IFaultQueue>? orchestrationEventsFaultQueue, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.OrchestrationEventsFaultQueue == null)
			_serviceBusConfiguration.OrchestrationEventsFaultQueue = orchestrationEventsFaultQueue;

		return _builder;
	}

	public TBuilder OrchestrationExchange(Action<ExchangeConfigurationBuilder<OrchestrationEvent>>? orchestrationExchange, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.OrchestrationExchange == null)
			_serviceBusConfiguration.OrchestrationExchange = orchestrationExchange;

		return _builder;
	}

	public TBuilder OrchestrationQueue(Action<MessageQueueConfigurationBuilder<OrchestrationEvent>>? orchestrationQueue, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.OrchestrationQueue == null)
			_serviceBusConfiguration.OrchestrationQueue = orchestrationQueue;

		return _builder;
	}
}

public class ServiceBusConfigurationBuilder : ServiceBusConfigurationBuilderBase<ServiceBusConfigurationBuilder, IServiceBusConfiguration>
{
	public ServiceBusConfigurationBuilder()
		: base(new ServiceBusConfiguration())
	{
	}

	public ServiceBusConfigurationBuilder(ServiceBusConfiguration serviceBusConfiguration)
		: base(serviceBusConfiguration)
	{
	}

	public static implicit operator ServiceBusConfiguration?(ServiceBusConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._serviceBusConfiguration as ServiceBusConfiguration;
	}

	public static implicit operator ServiceBusConfigurationBuilder?(ServiceBusConfiguration serviceBusConfiguration)
	{
		if (serviceBusConfiguration == null)
			return null;

		return new ServiceBusConfigurationBuilder(serviceBusConfiguration);
	}

	public static ServiceBusConfigurationBuilder GetDefaultBuilder()
		=> new ServiceBusConfigurationBuilder()
			.ServiceBusMode(Envelope.ServiceBus.ServiceBusMode.PublishSubscribe)
			//.HostInfo(null)
			//.ServiceBusName(null)
			//.MessageHandlerContextFactory(null)
			//.ExchangeProviderConfiguration(null)
			//.QueueProviderConfiguration(null)
			//.OrchestrationEventsFaultQueue(null)
			//.OrchestrationExchange(null)
			//.OrchestrationQueue(null)
			.MessageTypeResolver(sp => new FullNameTypeResolver())
			.HostLogger(sp => new DefaultHostLogger(sp.GetRequiredService<ILogger<DefaultHostLogger>>()))
			.HandlerLogger(sp => new DefaultHandlerLogger(sp.GetRequiredService<ILogger<DefaultHandlerLogger>>()))
			//.MessageHandlerResultFactory(sp => new MessageHandlerResultFactory())
			;
}
