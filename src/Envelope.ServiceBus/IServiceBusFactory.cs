using Envelope.ServiceBus.Configuration;
using Envelope.Trace;

namespace Envelope.ServiceBus;

public interface IServiceBusFactory
{
	IServiceBus Create(
		IServiceProvider serviceProvider,
		Action<ServiceBusConfigurationBuilder> configure,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	IServiceBus Create(
		IServiceProvider serviceProvider,
		IServiceBusConfiguration configuration,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	IServiceBus Create(
		IServiceBusOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);
}
