using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

public abstract class SyncStepBody : ISyncStepBody
{
	public BodyType BodyType => BodyType.Custom;

	public abstract IExecutionResult Run(IStepExecutionContext context);
}
