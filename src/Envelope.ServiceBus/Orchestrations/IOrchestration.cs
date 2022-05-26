using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Orchestrations.Definition.Builder;

namespace Envelope.ServiceBus.Orchestrations;

public interface IOrchestration<TData>
{
	Guid IdOrchestrationDefinition { get; }
	int Version { get; }
	string? Description { get; }
	bool IsSingleton { get; }
	bool AwaitForHandleLifeCycleEvents { get; }
	IErrorHandlingController? DefaultErrorHandling { get; }
	TimeSpan DefaultDistributedLockExpiration { get; }
	TimeSpan WorkerIdleTimeout { get; }
	void Build(OrchestrationBuilder<TData> builder);
}
