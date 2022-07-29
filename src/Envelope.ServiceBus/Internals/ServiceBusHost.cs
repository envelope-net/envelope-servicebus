using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Envelope.ServiceBus.Internals;

internal class ServiceBusHost : BackgroundService, IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private IServiceBus _serviceBus;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public ServiceBusHost(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_serviceBus = _serviceProvider.GetRequiredService<IServiceBus>();
		return Task.CompletedTask;
	}
}