using Envelope.Exceptions;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Envelope.ServiceBus.Configuration;

public interface IServiceBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IServiceBusConfigurationBuilder<TBuilder, TObject>
	where TObject : IServiceBusConfiguration
{
	TBuilder Object(TObject serviceBusConfiguration);

	TObject Build(bool finalize = false);

	TBuilder ServiceBusName(string serviceBusName, bool force = false);

	TBuilder MessageTypeResolver(Func<IServiceProvider, IMessageTypeResolver> messageTypeResolver, bool force = false);

	TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = false);

	TBuilder ExchangeProviderConfiguration(Action<ExchangeProviderConfigurationBuilder> exchangeProviderConfiguration, bool force = false);

	TBuilder QueueProviderConfiguration(Action<QueueProviderConfigurationBuilder> queueProviderConfiguration, bool force = false);

	TBuilder MessageHandlerContextFactory<TContext>(Func<IServiceProvider, TContext> messageHandlerContextFactory, bool force = false)
		where TContext : MessageHandlerContext;

	TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = false);

	TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = false);

	TBuilder AddServiceBusEventHandler(ServiceBusEventHandler serviceBusEventHandler);
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

		var error = _serviceBusConfiguration.Validate(nameof(IServiceBusConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _serviceBusConfiguration;
	}

	public TBuilder ServiceBusName(string serviceBusName, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_serviceBusConfiguration.ServiceBusName))
			_serviceBusConfiguration.ServiceBusName = serviceBusName;

		return _builder;
	}

	public TBuilder MessageTypeResolver(Func<IServiceProvider, IMessageTypeResolver> messageTypeResolver, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.MessageTypeResolver == null)
			_serviceBusConfiguration.MessageTypeResolver = messageTypeResolver;

		return _builder;
	}

	public TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.HostLogger == null)
			_serviceBusConfiguration.HostLogger = hostLogger;

		return _builder;
	}

	public TBuilder ExchangeProviderConfiguration(Action<ExchangeProviderConfigurationBuilder> exchangeProviderConfiguration, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.ExchangeProviderConfiguration == null)
			_serviceBusConfiguration.ExchangeProviderConfiguration = exchangeProviderConfiguration;

		return _builder;
	}

	public TBuilder QueueProviderConfiguration(Action<QueueProviderConfigurationBuilder> queueProviderConfiguration, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.QueueProviderConfiguration == null)
			_serviceBusConfiguration.QueueProviderConfiguration = queueProviderConfiguration;

		return _builder;
	}

	public TBuilder MessageHandlerContextFactory<TContext>(Func<IServiceProvider, TContext> messageHandlerContextFactory, bool force = false)
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

	public TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.HandlerLogger == null)
			_serviceBusConfiguration.HandlerLogger = handlerLogger;

		return _builder;
	}

	public TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _serviceBusConfiguration.MessageHandlerResultFactory == null)
			_serviceBusConfiguration.MessageHandlerResultFactory = messageHandlerResultFactory;

		return _builder;
	}

	public TBuilder AddServiceBusEventHandler(ServiceBusEventHandler serviceBusEventHandler)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (serviceBusEventHandler == null)
			throw new ArgumentNullException(nameof(serviceBusEventHandler));

		_serviceBusConfiguration.ServiceBusEventHandlers.Add(serviceBusEventHandler);
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

	internal static ServiceBusConfigurationBuilder GetDefaultBuilder()
		=> new ServiceBusConfigurationBuilder()
			//.ServiceBusName(null)
			//.MessageHandlerContextFactory(null)
			//.ExchangeProviderConfiguration(null)
			//.QueueProviderConfiguration(null)
			.MessageTypeResolver(sp => new FullNameTypeResolver())
			.HostLogger(sp => new HostLogger(sp.GetRequiredService<ILogger<HostLogger>>()))
			.HandlerLogger(sp => new HandlerLogger(sp.GetRequiredService<ILogger<HandlerLogger>>()))
			.MessageHandlerResultFactory(sp => new MessageHandlerResultFactory());
}
