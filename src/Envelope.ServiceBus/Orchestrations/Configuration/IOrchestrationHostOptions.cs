using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Logging;
using Envelope.Transactions;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public interface IOrchestrationHostOptions : IValidable
{
	IHostInfo HostInfo { get; }
	string HostName { get; }
	IOrchestrationRegistry OrchestrationRegistry { get; } //OrchestrationController, OrchestrationExecutor
	IDistributedLockProvider DistributedLockProvider { get; } //OrchestrationController, OrchestrationExecutor
	IErrorHandlingController ErrorHandlingController { get; }
	Func<IServiceProvider, IEventPublisher>? EventPublisherFactory { get; }
	Func<IServiceProvider, IOrchestrationRegistry, IOrchestrationRepository> OrchestrationRepositoryFactory { get; } //OrchestrationExecutor
	Func<IServiceProvider, IOrchestrationLogger> OrchestrationLogger { get; } //OrchestrationController, OrchestrationExecutor
	Func<IServiceProvider, IExecutionPointerFactory> ExecutionPointerFactory { get; } //OrchestrationController, OrchestrationExecutor
}
