using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps;

public interface IAsyncOrchestrationStep<TAsyncStepBody> : IOrchestrationStep
	where TAsyncStepBody : IAsyncStepBody
{
	TAsyncStepBody? ConstructStepBody(IServiceProvider serviceProvider);
}
