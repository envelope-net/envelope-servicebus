using Envelope.ServiceBus.Orchestrations.Definition;

namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class ExecutionPointerFactory : IExecutionPointerFactory
{
	public ExecutionPointer? BuildGenesisPointer(IOrchestrationDefinition orchestrationDefinition)
	{
		var nextStep = orchestrationDefinition.Steps.FirstOrDefault();
		if (nextStep == null)
			return null;

		return new ExecutionPointer(Guid.NewGuid(), nextStep)
		{
			Active = true,
			Status = PointerStatus.Pending
		};
	}

	public ExecutionPointer? BuildNextPointer(IOrchestrationDefinition orchestrationDefinition, IExecutionPointer previousPointer, Guid idNextStep)
	{
		var nextStep = orchestrationDefinition.Steps.FindById(idNextStep);
		if (nextStep == null)
			return null;

		return new ExecutionPointer(Guid.NewGuid(), nextStep)
		{
			PredecessorExecutionPointer = previousPointer,
			Active = true,
			Status = PointerStatus.Pending
		};
	}

	public ExecutionPointer? BuildNestedPointer(IOrchestrationDefinition orchestrationDefinition, IExecutionPointer previousPointer, Guid idNestedStep)
	{
		var nestedStep = orchestrationDefinition.Steps.FindById(idNestedStep);
		if (nestedStep == null)
			return null;

		var nestedExecutionPointer = new ExecutionPointer(Guid.NewGuid(), nestedStep)
		{
			PredecessorExecutionPointer = null, //set ContainerExecutionPointer instead in method AddNestedExecutionPointer
			Active = true,
			Status = PointerStatus.Pending
		};

		previousPointer.AddNestedExecutionPointer(nestedExecutionPointer);

		return nestedExecutionPointer;
	}
}
