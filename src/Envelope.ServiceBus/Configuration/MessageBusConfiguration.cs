using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public class MessageBusConfiguration : IMessageBusConfiguration, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string MessageBusName { get; set; }
	public IMessageTypeResolver MessageTypeResolver { get; set; }
	public Func<IServiceProvider, IHostLogger> HostLogger { get; set; }
	public Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }
	public List<IMessageHandlerType> MessageHandlerTypes { get; set; }
	public List<IMessageHandlersAssembly> MessageHandlerAssemblies { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(MessageBusName))
		{
			parentErrorBuffer ??= new List<IValidationMessage>();
			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageBusName))} == null"));
		}

		if (HostLogger == null)
		{
			parentErrorBuffer ??= new List<IValidationMessage>();
			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostLogger))} == null"));
		}

		if (HandlerLogger == null)
		{
			parentErrorBuffer ??= new List<IValidationMessage>();
			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HandlerLogger))} == null"));
		}

		if ((MessageHandlerTypes == null || MessageHandlerTypes.Count == 0) && (MessageHandlerAssemblies == null || MessageHandlerAssemblies.Count == 0))
		{
			parentErrorBuffer ??= new List<IValidationMessage>();
			parentErrorBuffer.Add(ValidationMessageFactory.Error(StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", $"{nameof(MessageHandlerTypes)} == null && {nameof(MessageHandlerAssemblies)} == null")));
		}

		return parentErrorBuffer;
	}
}
