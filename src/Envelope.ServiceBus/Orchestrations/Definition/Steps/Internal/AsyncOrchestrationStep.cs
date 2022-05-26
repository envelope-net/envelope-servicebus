using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;

internal class AsyncOrchestrationStep<TAsyncStepBody> : OrchestrationStep, IAsyncOrchestrationStep<TAsyncStepBody>, IOrchestrationStep
	where TAsyncStepBody : IAsyncStepBody
{
	public override Type? BodyType => typeof(TAsyncStepBody);

	public AsyncOrchestrationStep(string name)
		: base(name)
	{
	}

	public override IStepBody? ConstructBody(IServiceProvider serviceProvider)
		=> ConstructStepBody(serviceProvider);

	public virtual TAsyncStepBody? ConstructStepBody(IServiceProvider serviceProvider)
	{
		if (BodyType == null)
			return default;

		var body = (TAsyncStepBody?)serviceProvider.GetService(BodyType);

		if (body == null)
		{
			var stepCtor = BodyType.GetConstructor(Array.Empty<Type>());
			if (stepCtor != null)
				body = (TAsyncStepBody)stepCtor.Invoke(null);
		}
		return body;
	}
}
