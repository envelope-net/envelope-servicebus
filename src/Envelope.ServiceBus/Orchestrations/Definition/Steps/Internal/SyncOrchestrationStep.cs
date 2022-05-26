using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;

internal class SyncOrchestrationStep<TSyncStepBody> : OrchestrationStep, ISyncOrchestrationStep<TSyncStepBody>, IOrchestrationStep
	where TSyncStepBody : ISyncStepBody
{
	public override Type? BodyType => typeof(TSyncStepBody);

	public SyncOrchestrationStep(string name)
		: base(name)
	{
	}

	public override IStepBody? ConstructBody(IServiceProvider serviceProvider)
		=> ConstructStepBody(serviceProvider);

	public virtual TSyncStepBody? ConstructStepBody(IServiceProvider serviceProvider)
	{
		if (BodyType == null)
			return default;

		var body = (TSyncStepBody?)serviceProvider.GetService(BodyType);

		if (body == null)
		{
			var stepCtor = BodyType.GetConstructor(Array.Empty<Type>());
			if (stepCtor != null)
				body = (TSyncStepBody)stepCtor.Invoke(null);
		}
		return body;
	}
}
