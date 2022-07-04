using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Envelope.EventBus.Configuration;

public interface IEventBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IEventBusConfigurationBuilder<TBuilder, TObject>
	where TObject : IEventBusConfiguration
{
	TBuilder Object(TObject eventBusConfiguration);

	TObject Build(bool finalize = false);

	TBuilder EventBusName(string eventBusName, bool force = true);

	TBuilder EventTypeResolver(IMessageTypeResolver eventTypeResolver, bool force = true);

	TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = true);

	TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = true);

	TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = true);

	TBuilder EventBodyProvider(IMessageBodyProvider eventBodyProvider, bool force = true);

	TBuilder AddEventHandlerType(IEventHandlerType eventHandlerType, bool force = true);

	TBuilder AddEventHandlerTypes(IEnumerable<IEventHandlerType> eventHandlerTypes, bool force = true);

	TBuilder AddEventHandlerAssembly(IEventHandlersAssembly eventHandlersAssembly, bool force = true);

	TBuilder EventHandlerAssemblies(IEnumerable<IEventHandlersAssembly> eventHandlerAssemblies, bool force = true);
}

public abstract class EventBusConfigurationBuilderBase<TBuilder, TObject> : IEventBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : EventBusConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IEventBusConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _eventBusConfiguration;

	protected EventBusConfigurationBuilderBase(TObject eventBusConfiguration)
	{
		_eventBusConfiguration = eventBusConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject eventBusConfiguration)
	{
		_eventBusConfiguration = eventBusConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _eventBusConfiguration.Validate(nameof(IEventBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _eventBusConfiguration;
	}

	public TBuilder EventBusName(string eventBusName, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_eventBusConfiguration.EventBusName))
			_eventBusConfiguration.EventBusName = eventBusName;

		return _builder;
	}

	public TBuilder EventTypeResolver(IMessageTypeResolver eventTypeResolver, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _eventBusConfiguration.EventTypeResolver == null)
			_eventBusConfiguration.EventTypeResolver = eventTypeResolver;

		return _builder;
	}

	public TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _eventBusConfiguration.HostLogger == null)
			_eventBusConfiguration.HostLogger = hostLogger;

		return _builder;
	}

	public TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _eventBusConfiguration.HandlerLogger == null)
			_eventBusConfiguration.HandlerLogger = handlerLogger;

		return _builder;
	}

	public TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _eventBusConfiguration.MessageHandlerResultFactory == null)
			_eventBusConfiguration.MessageHandlerResultFactory = messageHandlerResultFactory;

		return _builder;
	}

	public TBuilder EventBodyProvider(IMessageBodyProvider? eventBodyProvider, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _eventBusConfiguration.EventBodyProvider == null)
			_eventBusConfiguration.EventBodyProvider = eventBodyProvider;

		return _builder;
	}

	public TBuilder AddEventHandlerType(IEventHandlerType eventHandlerType, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (eventHandlerType == null)
			throw new ArgumentNullException(nameof(eventHandlerType));

		if (_eventBusConfiguration.EventHandlerTypes == null || _eventBusConfiguration.EventHandlerTypes.Count == 0)
			_eventBusConfiguration.EventHandlerTypes = new List<IEventHandlerType> { eventHandlerType };
		else if (force || _eventBusConfiguration.EventHandlerTypes.All(x => x.HandlerType != eventHandlerType.HandlerType))
			_eventBusConfiguration.EventHandlerTypes.Add(eventHandlerType);

		return _builder;
	}

	public TBuilder AddEventHandlerTypes(IEnumerable<IEventHandlerType> eventHandlerTypes, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (eventHandlerTypes == null)
			throw new ArgumentNullException(nameof(eventHandlerTypes));

		if (force || _eventBusConfiguration.EventHandlerTypes == null || _eventBusConfiguration.EventHandlerTypes.Count == 0)
			_eventBusConfiguration.EventHandlerTypes = eventHandlerTypes.ToList();

		return _builder;
	}

	public TBuilder AddEventHandlerAssembly(IEventHandlersAssembly eventHandlersAssembly, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (eventHandlersAssembly == null)
			throw new ArgumentNullException(nameof(eventHandlersAssembly));

		if (_eventBusConfiguration.EventHandlerAssemblies == null || _eventBusConfiguration.EventHandlerAssemblies.Count == 0)
			_eventBusConfiguration.EventHandlerAssemblies = new List<IEventHandlersAssembly> { eventHandlersAssembly };
		else if (force || _eventBusConfiguration.EventHandlerAssemblies.All(x => x.HandlersAssembly != eventHandlersAssembly.HandlersAssembly))
			_eventBusConfiguration.EventHandlerAssemblies.Add(eventHandlersAssembly);

		return _builder;
	}

	public TBuilder EventHandlerAssemblies(IEnumerable<IEventHandlersAssembly> eventHandlerAssemblies, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (eventHandlerAssemblies == null)
			throw new ArgumentNullException(nameof(eventHandlerAssemblies));

		if (force || _eventBusConfiguration.EventHandlerAssemblies == null || _eventBusConfiguration.EventHandlerAssemblies.Count == 0)
			_eventBusConfiguration.EventHandlerAssemblies = eventHandlerAssemblies.ToList();

		return _builder;
	}
}

public class EventBusConfigurationBuilder : EventBusConfigurationBuilderBase<EventBusConfigurationBuilder, IEventBusConfiguration>
{
	public EventBusConfigurationBuilder()
		: base(new EventBusConfiguration())
	{
	}

	public EventBusConfigurationBuilder(EventBusConfiguration eventBusConfiguration)
		: base(eventBusConfiguration)
	{
	}

	public static implicit operator EventBusConfiguration?(EventBusConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._eventBusConfiguration as EventBusConfiguration;
	}

	public static implicit operator EventBusConfigurationBuilder?(EventBusConfiguration eventBusConfiguration)
	{
		if (eventBusConfiguration == null)
			return null;

		return new EventBusConfigurationBuilder(eventBusConfiguration);
	}

	internal static EventBusConfigurationBuilder GetDefaultBuilder()
		=> new EventBusConfigurationBuilder()
			//.EventTypeResolver()
			//.EventHandlerAssemblies()
			//.EventBusName(null)
			//.EventBodyProvider(null)
			.HostLogger(sp => new DefaultHostLogger(sp.GetRequiredService<ILogger<DefaultHostLogger>>()))
			.HandlerLogger(sp => new DefaultHandlerLogger(sp.GetRequiredService<ILogger<DefaultHandlerLogger>>()))
			.MessageHandlerResultFactory(sp => new MessageHandlerResultFactory());
}
