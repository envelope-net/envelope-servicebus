using Envelope.ServiceBus.Configuration;

namespace Envelope.ServiceBus.Internal;

public class ServiceBusFactory : IServiceBusFactory
{
	public IServiceBus Create(IServiceProvider serviceProvider, Action<ServiceBusConfigurationBuilder> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = ServiceBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);
		var configuration = builder.Build();
		var options = configuration.BuildOptions(serviceProvider);

		var serviceBus = new ServiceBus(options);
		return serviceBus;
	}
}
