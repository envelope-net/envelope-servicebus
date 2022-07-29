using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.ServiceBus.Jobs.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Envelope.ServiceBus.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddJobs(
		this IServiceCollection services,
		IHostInfo hostInfo,
		Action<JobProviderConfigurationBuilder>? configure = null)
	{
		if (services.Any(x => x.ServiceType == typeof(JobProviderConfigurationBuilder)))
			throw new InvalidOperationException("Job services already registered");

		services.TryAddSingleton(sp =>
		{
			var jobProviderConfigurationBuilder = JobProviderConfigurationBuilder.GetDefaultBuilder();
			configure?.Invoke(jobProviderConfigurationBuilder);

			var jobProviderConfiguration =
				jobProviderConfigurationBuilder
					.HostInfo(hostInfo)
					.Build(sp);

			return jobProviderConfiguration;
		});

		services.TryAddSingleton<IJobController, JobController>();

		return services;
	}
}
