using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps;

public interface ISyncOrchestrationStep<TStepBody> : IOrchestrationStep
	where TStepBody : ISyncStepBody
{
	TStepBody? ConstructStepBody(IServiceProvider serviceProvider);
}
