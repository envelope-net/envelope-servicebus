using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.Text;
using System.Text;

namespace Envelope.ServiceBus.Configuration;

public class EventBusConfiguration : IEventBusConfiguration
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string EventBusName { get; set; }
	public IMessageTypeResolver EventTypeResolver { get; set; }
	public Func<IServiceProvider, IHostLogger> HostLogger { get; set; }
	public Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }
	public Func<IServiceProvider, IMessageHandlerResultFactory> MessageHandlerResultFactory { get; set; }
	public IMessageBodyProvider? EventBodyProvider { get; set; }
	public List<IEventHandlerType> EventHandlerTypes { get; set; }
	public List<IEventHandlersAssembly> EventHandlerAssemblies { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(EventBusName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(EventBusName))} == null");
		}

		if (HostLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostLogger))} == null");
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

		if ((EventHandlerTypes == null || EventHandlerTypes.Count == 0) && (EventHandlerAssemblies == null || EventHandlerAssemblies.Count == 0))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine(StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", $"{nameof(EventHandlerTypes)} == null && {nameof(EventHandlerAssemblies)} == null"));
		}

		return parentErrorBuffer;
	}

	public IEventBusOptions BuildOptions(IServiceProvider serviceProvider)
	{
		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		var error = Validate(nameof(EventBusConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		var options = new EventBusOptions()
		{
			HostInfo = new HostInfo(EventBusName),
			HostLogger = HostLogger(serviceProvider),
			HandlerLogger = HandlerLogger(serviceProvider),
			MessageHandlerResultFactory = MessageHandlerResultFactory(serviceProvider),
			EventBodyProvider = EventBodyProvider
		};

		error = options.Validate(nameof(EventBusOptions))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return options;
	}
}
