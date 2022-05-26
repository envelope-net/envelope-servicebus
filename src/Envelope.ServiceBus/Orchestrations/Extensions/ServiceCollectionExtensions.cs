using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Configuration.Internal;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Execution.Internal;
using Envelope.ServiceBus.Orchestrations.Internal;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.ServiceBus.Orchestrations.Logging.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddOrchestrations(
		this IServiceCollection services,
		Action<OrchestrationHostConfigurationBuilder>? configure = null)
	{
		if (services.Any(x => x.ServiceType == typeof(OrchestrationHostConfiguration)))
			throw new InvalidOperationException("Orchestration services already registered");

		var orchestrationHostConfigurationBuilder = OrchestrationHostConfigurationBuilder.GetDefaultBuilder();
		configure?.Invoke(orchestrationHostConfigurationBuilder);
		var orchestrationHostConfiguration = orchestrationHostConfigurationBuilder.Build();

		var errorHandlingController = orchestrationHostConfiguration.ErrorHandlerConfigurationBuilder.Build().BuildErrorHandlingController();

		if (orchestrationHostConfiguration.RegisterAsHostedService)
			services.AddHostedService(sp => OrchestrationHost(sp, orchestrationHostConfiguration));
		else
			services.AddSingleton(sp => OrchestrationHost(sp, orchestrationHostConfiguration));

		services.AddSingleton<IOrchestrationRegistry, OrchestrationRegistry>();
		services.AddSingleton<IOrchestrationController, OrchestrationController>();
		services.AddSingleton(orchestrationHostConfiguration.DistributedLockProviderFactory);
		services.AddSingleton(errorHandlingController);

		if (orchestrationHostConfiguration.EventPublisherFactory != null)
			services.AddScoped(orchestrationHostConfiguration.EventPublisherFactory);

		services.AddTransient(orchestrationHostConfiguration.OrchestrationRepositoryFactory);
		services.AddTransient<IOrchestrationLogger, OrchestrationLogger>();
		services.AddTransient<IOrchestrationExecutor, OrchestrationExecutor>();
		services.AddTransient<IExecutionPointerFactory, ExecutionPointerFactory>();

		return services;
	}
	
	private static IOrchestrationHost OrchestrationHost(IServiceProvider sp, IOrchestrationHostConfiguration orchestrationHostConfiguration)
	{
		var controller = sp.GetRequiredService<IOrchestrationController>();
		var host = new OrchestrationHost(controller)
		{
			HostInfo = new HostInfo(orchestrationHostConfiguration.HostName),
		};
		return host;
	}
}
