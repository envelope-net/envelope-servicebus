using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public interface IOrchestrationHostConfiguration : IValidable
{
	bool RegisterAsHostedService { get; set; }
	Func<IServiceProvider, IOrchestrationRegistry> OrchestrationRegistry { get; set; }
	Func<IServiceProvider, IExecutionPointerFactory> ExecutionPointerFactory { get; set; }
	Func<IServiceProvider, IOrchestrationRegistry, IOrchestrationRepository> OrchestrationRepositoryFactory { get; set; }
	Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; set; }
	Func<IServiceProvider, IOrchestrationLogger> OrchestrationLogger { get; set; }
	Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; set; }
	ErrorHandlerConfigurationBuilder ErrorHandlerConfigurationBuilder { get; set; }
}
