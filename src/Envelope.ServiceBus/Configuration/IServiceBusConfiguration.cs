using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public interface IServiceBusConfiguration : IValidable
{
	string ServiceBusName { get; set; }

	Func<IServiceProvider, IMessageTypeResolver> MessageTypeResolver { get; set; }

	Func<IServiceProvider, IHostLogger> HostLogger { get; set; }

	Action<ExchangeProviderConfigurationBuilder> ExchangeProviderConfiguration { get; set; }

	Action<QueueProviderConfigurationBuilder> QueueProviderConfiguration { get; set; }

	Type MessageHandlerContextType { get; set; }

	Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; set; }

	Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }

	Func<IServiceProvider, IMessageHandlerResultFactory> MessageHandlerResultFactory { get; set; }

	List<ServiceBusEventHandler> ServiceBusEventHandlers { get; }

	IServiceBusOptions BuildOptions(IServiceProvider serviceProvider);
}
