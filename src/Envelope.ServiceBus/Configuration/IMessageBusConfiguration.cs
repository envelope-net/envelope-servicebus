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
public interface IMessageBusConfiguration : IValidable
{
	string MessageBusName { get; set; }

	IMessageTypeResolver MessageTypeResolver { get; set; }

	Func<IServiceProvider, IHostLogger> HostLogger { get; set; }

	Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }

	Func<IServiceProvider, IMessageHandlerResultFactory> MessageHandlerResultFactory { get; set; }

	IMessageBodyProvider? MessageBodyProvider { get; set; }

	List<IMessageHandlerType> MessageHandlerTypes { get; set; }

	List<IMessageHandlersAssembly> MessageHandlerAssemblies { get; set; }
}
