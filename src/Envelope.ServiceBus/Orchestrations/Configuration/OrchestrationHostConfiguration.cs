using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.Text;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public class OrchestrationHostConfiguration : IOrchestrationHostConfiguration, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public string HostName { get; set; }
	public bool RegisterAsHostedService { get; set; }
	public Func<IServiceProvider, IOrchestrationRepository> OrchestrationRepositoryFactory { get; set; }
	public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; set; }
	public Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; set; }
	public ErrorHandlerConfigurationBuilder ErrorHandlerConfigurationBuilder { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(HostName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostName))} == null");
		}

		if (OrchestrationRepositoryFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationRepositoryFactory))} == null");
		}

		if (DistributedLockProviderFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DistributedLockProviderFactory))} == null");
		}

		if (ErrorHandlerConfigurationBuilder == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ErrorHandlerConfigurationBuilder))} == null");
		}

		return parentErrorBuffer;
	}
}
