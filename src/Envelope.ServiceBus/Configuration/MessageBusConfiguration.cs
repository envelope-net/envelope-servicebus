using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
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

	public List<IValidationMessage>? Validate(
		string? propertyPrefix = null,
		ValidationBuilder? validationBuilder = null,
		Dictionary<string, object>? globalValidationContext = null,
		Dictionary<string, object>? customValidationContext = null)
	{
		validationBuilder ??= new ValidationBuilder();
		validationBuilder.SetValidationMessages(propertyPrefix, globalValidationContext)
			.IfNullOrWhiteSpace(MessageBusName)
			.IfNull(HostLogger)
			.IfNull(HandlerLogger)
			.If((MessageHandlerTypes == null || MessageHandlerTypes.Count == 0) && (MessageHandlerAssemblies == null || MessageHandlerAssemblies.Count == 0))
			;

		return validationBuilder.Build();
	}
}
