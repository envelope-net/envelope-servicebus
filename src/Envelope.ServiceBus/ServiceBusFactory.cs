using Envelope.ServiceBus.Configuration;

namespace Envelope.ServiceBus;

public class ServiceBusFactory : IServiceBusFactory
{
	public IServiceBus Create(IServiceProvider serviceProvider, Action<ServiceBusConfigurationBuilder> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = ServiceBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);
		var configuration = builder.Build();

		var serviceBus = new ServiceBus(serviceProvider, configuration);
		return serviceBus;
	}

	public IServiceBus Create(IServiceProvider serviceProvider, IServiceBusConfiguration configuration)
		=> new ServiceBus(serviceProvider, configuration);

	public IServiceBus Create(IServiceBusOptions options)
	{
		if (options == null)
			throw new ArgumentNullException(nameof(options));

		var serviceBus = new ServiceBus(options);
		return serviceBus;
	}
}
