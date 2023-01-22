using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IAsyncStepBody : IStepBody
{
	Task<IExecutionResult> RunAsync(IStepExecutionContext context);
}
