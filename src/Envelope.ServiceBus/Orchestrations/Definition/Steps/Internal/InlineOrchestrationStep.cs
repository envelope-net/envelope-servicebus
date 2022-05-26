using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;

internal class InlineOrchestrationStep : SyncOrchestrationStep<SyncInlineStepBody>
{
	public Func<IStepExecutionContext, IExecutionResult> Body { get; set; }

	public InlineOrchestrationStep(Func<IStepExecutionContext, IExecutionResult> body)
		: base("SyncInline")
	{
		Body = body ?? throw new ArgumentNullException(nameof(body));
	}

	public override SyncInlineStepBody? ConstructStepBody(IServiceProvider serviceProvider)
		=> new(Body);
}
