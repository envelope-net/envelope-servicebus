using Envelope.Exceptions;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.Text;
using Envelope.Transactions;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Configuration.Internal;

internal class OrchestrationHostOptions : IOrchestrationHostOptions, IValidable
{
	public IHostInfo HostInfo { get; }
	public string HostName { get; }
	public ITransactionManagerFactory TransactionManagerFactory { get; }
	public Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; }
	public IOrchestrationRegistry OrchestrationRegistry { get; set; }
	public IDistributedLockProvider DistributedLockProvider { get; }
	public IErrorHandlingController ErrorHandlingController { get; }
	public Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; }
	public Func<IServiceProvider, IOrchestrationRegistry, IOrchestrationRepository> OrchestrationRepositoryFactory { get; }
	public Func<IServiceProvider, IOrchestrationLogger> OrchestrationLogger { get; }
	public Func<IServiceProvider, IExecutionPointerFactory> ExecutionPointerFactory { get; }

	public OrchestrationHostOptions(IOrchestrationHostConfiguration config, IHostInfo hostInfo, IServiceProvider serviceProvider)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		var error = config.Validate(nameof(OrchestrationHostConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
		HostName = hostInfo.HostName;
		TransactionManagerFactory = config.TransactionManagerFactory;
		TransactionContextFactory = config.TransactionContextFactory;
		OrchestrationRegistry = config.OrchestrationRegistry(serviceProvider);
		ExecutionPointerFactory = config.ExecutionPointerFactory;
		OrchestrationRepositoryFactory = config.OrchestrationRepositoryFactory;
		DistributedLockProvider = config.DistributedLockProviderFactory(serviceProvider);
		OrchestrationLogger = config.OrchestrationLogger;
		EventPublisherFactory = config.EventPublisherFactory;
		ErrorHandlingController = config.ErrorHandlerConfigurationBuilder.Build().BuildErrorHandlingController();
	}

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (HostInfo == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostInfo))} == null"));
		}

		if (string.IsNullOrWhiteSpace(HostName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostName))} == null"));
		}

		if (OrchestrationRegistry == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationRegistry))} == null"));
		}

		if (ExecutionPointerFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExecutionPointerFactory))} == null"));
		}

		if (OrchestrationRepositoryFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationRepositoryFactory))} == null"));
		}

		if (DistributedLockProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DistributedLockProvider))} == null"));
		}

		if (OrchestrationLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(OrchestrationLogger))} == null"));
		}

		if (EventPublisherFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(EventPublisherFactory))} == null"));
		}

		if (ErrorHandlingController == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ErrorHandlingController))} == null"));
		}

		return parentErrorBuffer;
	}
}
