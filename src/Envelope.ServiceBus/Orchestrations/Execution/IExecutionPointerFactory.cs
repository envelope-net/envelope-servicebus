using Envelope.ServiceBus.Orchestrations.Definition;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IExecutionPointerFactory
{
	ExecutionPointer? BuildGenesisPointer(IOrchestrationDefinition def);

	ExecutionPointer? BuildNextPointer(IOrchestrationDefinition def, IExecutionPointer previousPointer, Guid idNextStep);

	ExecutionPointer? BuildNestedPointer(IOrchestrationDefinition orchestrationDefinition, IExecutionPointer previousPointer, Guid idNestedStep);
}
