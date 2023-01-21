using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessageBusOptions : IValidable
{
	IHostInfo HostInfo { get; }
	IHostLogger HostLogger { get; }
	IHandlerLogger HandlerLogger { get; }
	IMessageBodyProvider? MessageBodyProvider { get; }
	IMessageHandlerResultFactory MessageHandlerResultFactory { get; }
}
