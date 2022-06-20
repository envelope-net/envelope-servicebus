namespace Envelope.ServiceBus.Orchestrations.Execution.Internal;

internal class ExecutionPointerFactory : IExecutionPointerFactory
{
	public ExecutionPointer BuildGenesisPointer(IOrchestrationInstance orchestrationInstance)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		var nextStep = orchestrationInstance.GetOrchestrationDefinition().Steps.FirstOrDefault();
		if (nextStep == null)
			throw new InvalidOperationException("No steps defined");

		var id = Guid.NewGuid();
		return new ExecutionPointer(id, orchestrationInstance.IdOrchestrationInstance, orchestrationInstance.IdOrchestrationDefinition, orchestrationInstance.Version, nextStep)
		.Update(new ExecutionPointerUpdate(id)
		{
			Active = true,
			Status = PointerStatus.Pending
		});
	}

	public ExecutionPointer? BuildNextPointer(IOrchestrationInstance orchestrationInstance, IExecutionPointer previousPointer, Guid idNextStep)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		var nextStep = orchestrationInstance.GetOrchestrationDefinition().Steps.FindById(idNextStep);
		if (nextStep == null)
			return null;

		var id = Guid.NewGuid();
		return new ExecutionPointer(id, orchestrationInstance.IdOrchestrationInstance, orchestrationInstance.IdOrchestrationDefinition, orchestrationInstance.Version, nextStep)
		.Update(new ExecutionPointerUpdate(id)
		{
			PredecessorExecutionPointerStartingStepId = previousPointer?.GetStep().StartingStep?.IdStep,
			PredecessorExecutionPointerId = previousPointer?.IdExecutionPointer,
			Active = true,
			Status = PointerStatus.Pending
		});
	}

	public ExecutionPointer? BuildNestedPointer(IOrchestrationInstance orchestrationInstance, IExecutionPointer previousPointer, Guid idNestedStep)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		var nestedStep = orchestrationInstance.GetOrchestrationDefinition().Steps.FindById(idNestedStep);
		if (nestedStep == null)
			return null;

		var id = Guid.NewGuid();
		var nestedExecutionPointer = new ExecutionPointer(id, orchestrationInstance.IdOrchestrationInstance, orchestrationInstance.IdOrchestrationDefinition, orchestrationInstance.Version, nestedStep)
		.Update(new ExecutionPointerUpdate(id)
		{
			PredecessorExecutionPointerStartingStepId = null,
			Active = true,
			Status = PointerStatus.Pending
		});

		return nestedExecutionPointer;
	}
}
