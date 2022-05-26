using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

internal class AsyncInlineStepBody : IAsyncInlineStepBody, IAsyncStepBody, IStepBody
{
	private readonly Func<IStepExecutionContext, Task<IExecutionResult>> _body;

	public BodyType BodyType => BodyType.Inline;

	public AsyncInlineStepBody(Func<IStepExecutionContext, Task<IExecutionResult>> body)
	{
		_body = body ?? throw new ArgumentNullException(nameof(body));
	}

	public Task<IExecutionResult> RunAsync(IStepExecutionContext context)
		=> _body.Invoke(context);
}
