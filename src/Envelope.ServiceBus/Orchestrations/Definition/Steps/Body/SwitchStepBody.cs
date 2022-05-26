using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

internal class SwitchStepBody : ISyncStepBody, IStepBody
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public Func<IStepExecutionContext, object> Case { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public BodyType BodyType => BodyType.Switch;

	public IExecutionResult Run(IStepExecutionContext context)
	{
		var branchIds = context.Step.Branches.Select(x => x.Value.IdStep).ToList();
		var finalizedBranchesCount = context.Orchestration.FinalizedBranches.Count(x => branchIds.Contains(x.IdStep));

		if (0 < finalizedBranchesCount)
			return ExecutionResultFactory.NextStep();

		var result = Case(context);
		if (context.Step.Branches.TryGetValue(result, out var step))
			return ExecutionResultFactory.BranchSteps(new List<Guid> { step.IdStep });
		else
			return ExecutionResultFactory.NextStep();
	}
}
