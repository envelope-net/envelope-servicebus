using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public class EventBusConfiguration : IEventBusConfiguration, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string EventBusName { get; set; }
	public IMessageTypeResolver EventTypeResolver { get; set; }
	public Func<IServiceProvider, IHostLogger> HostLogger { get; set; }
	public Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }
	public List<IEventHandlerType> EventHandlerTypes { get; set; }
	public List<IEventHandlersAssembly> EventHandlerAssemblies { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public List<IValidationMessage>? Validate(
		string? propertyPrefix = null,
		ValidationBuilder? validationBuilder = null,
		Dictionary<string, object>? globalValidationContext = null,
		Dictionary<string, object>? customValidationContext = null)
	{
		validationBuilder ??= new ValidationBuilder();
		validationBuilder.SetValidationMessages(propertyPrefix, globalValidationContext)
			.IfNullOrWhiteSpace(EventBusName)
			.IfNull(HostLogger)
			.IfNull(HandlerLogger)
			.If((EventHandlerTypes == null || EventHandlerTypes.Count == 0) && (EventHandlerAssemblies == null || EventHandlerAssemblies.Count == 0))
			;

		return validationBuilder.Build();
	}
}
