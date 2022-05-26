using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public interface IEventBusOptions : IValidable
{
	IHostInfo HostInfo { get; }
	IHostLogger HostLogger { get; }
	IHandlerLogger HandlerLogger { get; }
	IMessageBodyProvider? EventBodyProvider { get; }
	IMessageHandlerResultFactory MessageHandlerResultFactory { get; }
}
