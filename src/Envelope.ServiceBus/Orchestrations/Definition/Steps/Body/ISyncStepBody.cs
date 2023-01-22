using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ISyncStepBody : IStepBody
{
	IExecutionResult Run(IStepExecutionContext context);
}
