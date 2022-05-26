using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

internal class WhileStepBody : ISyncStepBody, IStepBody
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public Func<IStepExecutionContext, bool> Condition { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public BodyType BodyType => BodyType.While;

	public IExecutionResult Run(IStepExecutionContext context)
	{
		if (Condition(context))
			return ExecutionResultFactory.BranchSteps(new List<Guid> { context.Step.Branches[true].IdStep });
		else
			return ExecutionResultFactory.NextStep();
	}
}
