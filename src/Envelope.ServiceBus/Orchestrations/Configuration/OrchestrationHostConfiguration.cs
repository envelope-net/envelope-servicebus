using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.Text;
using Envelope.Transactions;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public class OrchestrationHostConfiguration : IOrchestrationHostConfiguration, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public bool RegisterAsHostedService { get; set; }
	public ITransactionManagerFactory TransactionManagerFactory { get; set; }
	public Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; set; }
	public Func<IServiceProvider, IOrchestrationRegistry> OrchestrationRegistry { get; set; }
	public Func<IServiceProvider, IExecutionPointerFactory> ExecutionPointerFactory { get; set; }
	public Func<IServiceProvider, IOrchestrationRegistry, IOrchestrationRepository> OrchestrationRepositoryFactory { get; set; }
	public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; set; }
	public Func<IServiceProvider, IOrchestrationLogger> OrchestrationLogger { get; set; }
	public Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; set; }
	public ErrorHandlerConfigurationBuilder ErrorHandlerConfigurationBuilder { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (TransactionManagerFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionManagerFactory))} == null");
		}

		if (TransactionContextFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionContextFactory))} == null");
		}

		if (OrchestrationRegistry == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationRegistry))} == null");
		}

		if (ExecutionPointerFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExecutionPointerFactory))} == null");
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

		if (OrchestrationLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationLogger))} == null");
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
