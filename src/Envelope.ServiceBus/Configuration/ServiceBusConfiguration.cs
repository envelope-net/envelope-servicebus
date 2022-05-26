using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Exchange.Internal;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.ServiceBus.Queues.Internal;
using Envelope.Text;
using System.Text;

namespace Envelope.ServiceBus.Configuration;

public class ServiceBusConfiguration : IServiceBusConfiguration
{
	public string ServiceBusName { get; set; }
	public Func<IServiceProvider, IMessageTypeResolver> MessageTypeResolver { get; set; }
	public Func<IServiceProvider, IHostLogger> HostLogger { get; set; }
	public Action<ExchangeProviderConfigurationBuilder> ExchangeProviderConfiguration { get; set; }
	public Action<QueueProviderConfigurationBuilder> QueueProviderConfiguration { get; set; }
	public Type MessageHandlerContextType { get; set; }
	public Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; set; }
	public Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }
	public Func<IServiceProvider, IMessageHandlerResultFactory> MessageHandlerResultFactory { get; set; }
	public List<ServiceBusEventHandler> ServiceBusEventHandlers { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public ServiceBusConfiguration()
	{
		ServiceBusEventHandlers = new List<ServiceBusEventHandler>();
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(ServiceBusName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ServiceBusName))} == null");
		}

		if (MessageTypeResolver == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageTypeResolver))} == null");
		}

		if (HostLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostLogger))} == null");
		}

		//kvoli tomu, ze sa neskor automaticky prida exchange pre orchestracie
		//if (ExchangeProviderConfiguration == null)
		//{
		//	if (parentErrorBuffer == null)
		//		parentErrorBuffer = new StringBuilder();

		//	parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeProviderConfiguration))} == null");
		//}

		//kvoli tomu, ze sa neskor automaticky prida queue pre orchestracie
		//if (QueueProviderConfiguration == null)
		//{
		//	if (parentErrorBuffer == null)
		//		parentErrorBuffer = new StringBuilder();

		//	parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(QueueProviderConfiguration))} == null");
		//}

		if (MessageHandlerContextType == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerContextType))} == null");
		}

		if (MessageHandlerContextFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerContextFactory))} == null");
		}

		if (HandlerLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HandlerLogger))} == null");
		}

		if (MessageHandlerResultFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerResultFactory))} == null");
		}

		return parentErrorBuffer;
	}

	public IServiceBusOptions BuildOptions(IServiceProvider serviceProvider)
	{
		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		var error = Validate(nameof(ServiceBusConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		var options = new ServiceBusOptions(serviceProvider)
		{
			HostInfo = new HostInfo(ServiceBusName),
			MessageTypeResolver = MessageTypeResolver(serviceProvider),
			HostLogger = HostLogger(serviceProvider),
			MessageHandlerContextType = MessageHandlerContextType,
			MessageHandlerContextFactory = MessageHandlerContextFactory,
			HandlerLogger = HandlerLogger(serviceProvider),
			MessageHandlerResultFactory = MessageHandlerResultFactory(serviceProvider)
		};

		foreach (var handler in ServiceBusEventHandlers)
			options.ServiceBusLifeCycleEventManager.OnServiceBusEvent += handler;

		var exchangeProviderBuilder = ExchangeProviderConfigurationBuilder.GetDefaultBuilder(options);
		ExchangeProviderConfiguration?.Invoke(exchangeProviderBuilder);
		var exchangeProviderConfiguration = exchangeProviderBuilder.Build();
		var exchangeProvider = new ExchangeProvider(serviceProvider, exchangeProviderConfiguration, options);

		var queueProviderBuilder = QueueProviderConfigurationBuilder.GetDefaultBuilder(options);
		QueueProviderConfiguration?.Invoke(queueProviderBuilder);
		var queueProviderConfiguration = queueProviderBuilder.Build();
		var queueProvider = new QueueProvider(serviceProvider, queueProviderConfiguration, options);

		options.ExchangeProvider = exchangeProvider;
		options.QueueProvider = queueProvider;

		error = options.Validate(nameof(ServiceBusOptions))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return options;
	}
}
