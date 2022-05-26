using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public interface IOrchestrationHostConfiguration : IValidable
{
	string HostName { get; set; }
	bool RegisterAsHostedService { get; set; }
	Func<IServiceProvider, IOrchestrationRepository> OrchestrationRepositoryFactory { get; set; }
	Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; set; }
	Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; set; }
	ErrorHandlerConfigurationBuilder ErrorHandlerConfigurationBuilder { get; set; }
}
