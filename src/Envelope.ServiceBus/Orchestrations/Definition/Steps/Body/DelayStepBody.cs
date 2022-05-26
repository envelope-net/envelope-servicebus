using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

internal class DelayStepBody : ISyncStepBody, IStepBody
{	
	public TimeSpan DelayInterval { get; set; }

	public BodyType BodyType => BodyType.Delay;

	public IExecutionResult Run(IStepExecutionContext context)
		=> ExecutionResultFactory.Delay(DelayInterval);
}
