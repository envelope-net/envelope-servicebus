namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IAsyncInlineStepBody : IAsyncStepBody, IStepBody
{
}
