using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Graphing;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Definition;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IOrchestrationDefinition : IValidable
{
	Guid IdOrchestrationDefinition { get; }

	int Version { get; }

	string? Description { get; set; }

	bool IsSingleton { get; set; }

	bool AwaitForHandleLifeCycleEvents { get; }

	IReadOnlyOrchestrationStepCollection Steps { get; }

	Type DataType { get; }

	IErrorHandlingController DefaultErrorHandling { get; }

	TimeSpan DefaultDistributedLockExpiration { get; }

	TimeSpan WorkerIdleTimeout { get; }

	void AddStepInternal(IOrchestrationStep step);

	IOrchestrationInstance? GetOrSetSingletonInstanceInternal(Func<IOrchestrationInstance> orchestrationInstanceFactory, string orchestrationKey);

	IOrchestrationGraph ToGraph();
}
