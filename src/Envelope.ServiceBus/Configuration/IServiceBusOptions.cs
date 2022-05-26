using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Queues;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public interface IServiceBusOptions : IValidable
{
	IServiceProvider ServiceProvider { get; }
	IHostInfo HostInfo { get; }
	IMessageTypeResolver MessageTypeResolver { get; }
	IHostLogger HostLogger { get; }
	IExchangeProvider ExchangeProvider { get; }
	IQueueProvider QueueProvider { get; }
	Type MessageHandlerContextType { get; }
	Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; }
	IHandlerLogger HandlerLogger { get; }
	IMessageHandlerResultFactory MessageHandlerResultFactory { get; }
	IServiceBusLifeCycleEventManager ServiceBusLifeCycleEventManager { get; }
}
