using Envelope.ServiceBus.Configuration;
using Envelope.Trace;

namespace Envelope.ServiceBus;

public class ServiceBusFactory : IServiceBusFactory
{
	public IServiceBus Create(
		IServiceProvider serviceProvider,
		Action<ServiceBusConfigurationBuilder> configure,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = ServiceBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);
		var configuration = builder.Build();

		var serviceBus = new ServiceBus(serviceProvider, configuration);
		serviceBus.Initialize(traceInfo, cancellationToken);
		return serviceBus;
	}

	public IServiceBus Create(
		IServiceProvider serviceProvider,
		IServiceBusConfiguration configuration,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var serviceBus = new ServiceBus(serviceProvider, configuration);
		serviceBus.Initialize(traceInfo, cancellationToken);
		return serviceBus;
	}

	public IServiceBus Create(
		IServiceBusOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (options == null)
			throw new ArgumentNullException(nameof(options));

		var serviceBus = new ServiceBus(options);
		serviceBus.Initialize(traceInfo, cancellationToken);
		return serviceBus;
	}
}
