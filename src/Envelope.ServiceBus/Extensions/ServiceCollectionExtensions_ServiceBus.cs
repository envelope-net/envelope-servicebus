using Envelope.ServiceBus.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Extensions;

public static partial class ServiceCollectionExtensions
{
	public static IServiceCollection AddServiceBus(this IServiceCollection services)
	{
		services.AddHostedService<ServiceBusHost>();
		return services;
	}
}
