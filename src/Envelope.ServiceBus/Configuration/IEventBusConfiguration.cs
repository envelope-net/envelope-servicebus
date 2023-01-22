using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IEventBusConfiguration : IValidable
{
	string EventBusName { get; set; }

	IMessageTypeResolver EventTypeResolver { get; set; }

	Func<IServiceProvider, IHostLogger> HostLogger { get; set; }

	Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }

	Func<IServiceProvider, IMessageHandlerResultFactory> MessageHandlerResultFactory { get; set; }

	IMessageBodyProvider? EventBodyProvider { get; set; }

	List<IEventHandlerType> EventHandlerTypes { get; set; }

	List<IEventHandlersAssembly> EventHandlerAssemblies { get; set; }
}
