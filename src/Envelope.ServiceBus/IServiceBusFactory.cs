using Envelope.ServiceBus.Configuration;

namespace Envelope.ServiceBus;

public interface IServiceBusFactory
{
	IServiceBus Create(IServiceProvider serviceProvider, Action<ServiceBusConfigurationBuilder> configure);

	IServiceBus Create(IServiceProvider serviceProvider, IServiceBusConfiguration configuration);

	IServiceBus Create(IServiceBusOptions options);
}
