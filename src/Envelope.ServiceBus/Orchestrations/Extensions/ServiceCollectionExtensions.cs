using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Configuration.Internal;
using Envelope.ServiceBus.Orchestrations.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddOrchestrations(
		this IServiceCollection services,
		IHostInfo hostInfo,
		Action<OrchestrationHostConfigurationBuilder>? configure = null)
	{
		if (services.Any(x => x.ServiceType == typeof(OrchestrationHostConfiguration)))
			throw new InvalidOperationException("Orchestration services already registered");

		var orchestrationHostConfigurationBuilder = OrchestrationHostConfigurationBuilder.GetDefaultBuilder();
		configure?.Invoke(orchestrationHostConfigurationBuilder);
		var orchestrationHostConfiguration = orchestrationHostConfigurationBuilder.Build();

		services.AddSingleton<IOrchestrationHostOptions>(sp =>
		{
			var orchestrationOptions = new OrchestrationHostOptions(orchestrationHostConfiguration, hostInfo, sp);
			return orchestrationOptions;
		});

		if (orchestrationHostConfiguration.RegisterAsHostedService)
			services.AddHostedService<IOrchestrationHost>();
		else
			services.AddSingleton<IOrchestrationHost, OrchestrationHost>();

		services.AddTransient<IOrchestrationRepository>(sp =>
		{
			var options = sp.GetRequiredService<IOrchestrationHostOptions>();
			return options.OrchestrationRepositoryFactory(sp, options.OrchestrationRegistry);
		});

		return services;
	}
}
