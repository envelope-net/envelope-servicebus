using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

public interface ISyncStepBody : IStepBody
{
	IExecutionResult Run(IStepExecutionContext context);
}
