using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IServiceBusConfiguration : IValidable
{
	ServiceBusMode? ServiceBusMode { get; set; }

	IHostInfo HostInfo { get; set; }

	string ServiceBusName { get; set; }

	Func<IServiceProvider, IMessageTypeResolver> MessageTypeResolver { get; set; }

	Func<IServiceProvider, IHostLogger> HostLogger { get; set; }

	Action<ExchangeProviderConfigurationBuilder> ExchangeProviderConfiguration { get; set; }

	Action<QueueProviderConfigurationBuilder> QueueProviderConfiguration { get; set; }

	Type MessageHandlerContextType { get; set; }

	Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; set; }

	Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }

	List<ServiceBusEventHandler> ServiceBusEventHandlers { get; }

	Func<IServiceProvider, IFaultQueue>? OrchestrationEventsFaultQueue { get; set; }
	
	Action<ExchangeConfigurationBuilder<OrchestrationEvent>>? OrchestrationExchange { get; set; }

	Action<MessageQueueConfigurationBuilder<OrchestrationEvent>>? OrchestrationQueue { get; set; }
}
