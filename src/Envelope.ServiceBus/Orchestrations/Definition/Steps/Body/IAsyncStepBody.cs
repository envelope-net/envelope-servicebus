using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

public interface IAsyncStepBody : IStepBody
{
	Task<IExecutionResult> RunAsync(IStepExecutionContext context);
}
