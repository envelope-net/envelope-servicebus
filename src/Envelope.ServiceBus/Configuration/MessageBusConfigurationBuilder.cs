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

namespace Envelope.MessageBus.Configuration;

public interface IMessageBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IMessageBusConfigurationBuilder<TBuilder, TObject>
	where TObject : IMessageBusConfiguration
{
	TBuilder Object(TObject messageBusConfiguration);

	TObject Build(bool finalize = false);

	TBuilder MessageBusName(string messageBusName, bool force = false);

	TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = false);

	TBuilder MessageTypeResolver(IMessageTypeResolver messageTypeResolver, bool force = false);

	TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = false);

	TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = false);

	TBuilder MessageBodyProvider(IMessageBodyProvider messageBodyProvider, bool force = false);

	TBuilder AddMessageHandlerType(IMessageHandlerType messageHandlerType, bool force = false);

	TBuilder AddMessageHandlerTypes(IEnumerable<IMessageHandlerType> messageHandlerTypes, bool force = false);

	TBuilder AddMessageHandlerAssembly(IMessageHandlersAssembly messageHandlersAssembly, bool force = false);

	TBuilder MessageHandlerAssemblies(IEnumerable<IMessageHandlersAssembly> messageHandlerAssemblies, bool force = false);
}

public abstract class MessageBusConfigurationBuilderBase<TBuilder, TObject> : IMessageBusConfigurationBuilder<TBuilder, TObject>
	where TBuilder : MessageBusConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IMessageBusConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _messageBusConfiguration;

	protected MessageBusConfigurationBuilderBase(TObject messageBusConfiguration)
	{
		_messageBusConfiguration = messageBusConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject messageBusConfiguration)
	{
		_messageBusConfiguration = messageBusConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _messageBusConfiguration.Validate(nameof(IMessageBusConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _messageBusConfiguration;
	}

	public TBuilder MessageBusName(string messageBusName, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_messageBusConfiguration.MessageBusName))
			_messageBusConfiguration.MessageBusName = messageBusName;

		return _builder;
	}

	public TBuilder MessageTypeResolver(IMessageTypeResolver messageTypeResolver, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageBusConfiguration.MessageTypeResolver == null)
			_messageBusConfiguration.MessageTypeResolver = messageTypeResolver;

		return _builder;
	}

	public TBuilder HostLogger(Func<IServiceProvider, IHostLogger> hostLogger, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageBusConfiguration.HostLogger == null)
			_messageBusConfiguration.HostLogger = hostLogger;

		return _builder;
	}

	public TBuilder HandlerLogger(Func<IServiceProvider, IHandlerLogger> handlerLogger, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageBusConfiguration.HandlerLogger == null)
			_messageBusConfiguration.HandlerLogger = handlerLogger;

		return _builder;
	}

	public TBuilder MessageHandlerResultFactory(Func<IServiceProvider, IMessageHandlerResultFactory> messageHandlerResultFactory, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageBusConfiguration.MessageHandlerResultFactory == null)
			_messageBusConfiguration.MessageHandlerResultFactory = messageHandlerResultFactory;

		return _builder;
	}

	public TBuilder MessageBodyProvider(IMessageBodyProvider? messageBodyProvider, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageBusConfiguration.MessageBodyProvider == null)
			_messageBusConfiguration.MessageBodyProvider = messageBodyProvider;

		return _builder;
	}

	public TBuilder AddMessageHandlerType(IMessageHandlerType messageHandlerType, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (messageHandlerType == null)
			throw new ArgumentNullException(nameof(messageHandlerType));

		if (_messageBusConfiguration.MessageHandlerTypes == null || _messageBusConfiguration.MessageHandlerTypes.Count == 0)
			_messageBusConfiguration.MessageHandlerTypes = new List<IMessageHandlerType> { messageHandlerType };
		else if (force || _messageBusConfiguration.MessageHandlerTypes.All(x => x.HandlerType != messageHandlerType.HandlerType))
			_messageBusConfiguration.MessageHandlerTypes.Add(messageHandlerType);

		return _builder;
	}

	public TBuilder AddMessageHandlerTypes(IEnumerable<IMessageHandlerType> messageHandlerTypes, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (messageHandlerTypes == null)
			throw new ArgumentNullException(nameof(messageHandlerTypes));

		if (force || _messageBusConfiguration.MessageHandlerTypes == null || _messageBusConfiguration.MessageHandlerTypes.Count == 0)
			_messageBusConfiguration.MessageHandlerTypes = messageHandlerTypes.ToList();

		return _builder;
	}

	public TBuilder AddMessageHandlerAssembly(IMessageHandlersAssembly messageHandlersAssembly, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (messageHandlersAssembly == null)
			throw new ArgumentNullException(nameof(messageHandlersAssembly));

		if (_messageBusConfiguration.MessageHandlerAssemblies == null || _messageBusConfiguration.MessageHandlerAssemblies.Count == 0)
			_messageBusConfiguration.MessageHandlerAssemblies = new List<IMessageHandlersAssembly> { messageHandlersAssembly };
		else if (force || _messageBusConfiguration.MessageHandlerAssemblies.All(x => x.HandlersAssembly != messageHandlersAssembly.HandlersAssembly))
			_messageBusConfiguration.MessageHandlerAssemblies.Add(messageHandlersAssembly);

		return _builder;
	}

	public TBuilder MessageHandlerAssemblies(IEnumerable<IMessageHandlersAssembly> messageHandlerAssemblies, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (messageHandlerAssemblies == null)
			throw new ArgumentNullException(nameof(messageHandlerAssemblies));

		if (force || _messageBusConfiguration.MessageHandlerAssemblies == null || _messageBusConfiguration.MessageHandlerAssemblies.Count == 0)
			_messageBusConfiguration.MessageHandlerAssemblies = messageHandlerAssemblies.ToList();

		return _builder;
	}
}

public class MessageBusConfigurationBuilder : MessageBusConfigurationBuilderBase<MessageBusConfigurationBuilder, IMessageBusConfiguration>
{
	public MessageBusConfigurationBuilder()
		: base(new MessageBusConfiguration())
	{
	}

	public MessageBusConfigurationBuilder(MessageBusConfiguration messageBusConfiguration)
		: base(messageBusConfiguration)
	{
	}

	public static implicit operator MessageBusConfiguration?(MessageBusConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._messageBusConfiguration as MessageBusConfiguration;
	}

	public static implicit operator MessageBusConfigurationBuilder?(MessageBusConfiguration messageBusConfiguration)
	{
		if (messageBusConfiguration == null)
			return null;

		return new MessageBusConfigurationBuilder(messageBusConfiguration);
	}

	internal static MessageBusConfigurationBuilder GetDefaultBuilder()
		=> new MessageBusConfigurationBuilder()
			//.MessageBusName(null)
			//.MessageHandlerContextFactory(null)
			//.MessageBodyProvider(null)
			.HostLogger(sp => new HostLogger(sp.GetRequiredService<ILogger<HostLogger>>()))
			.HandlerLogger(sp => new HandlerLogger(sp.GetRequiredService<ILogger<HandlerLogger>>()))
			.MessageHandlerResultFactory(sp => new MessageHandlerResultFactory());
}
