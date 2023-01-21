namespace Envelope.ServiceBus.Orchestrations.Execution;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IReadOnlyExecutionPointerCollection : IReadOnlyCollection<ExecutionPointer>
{
	ExecutionPointer? FindById(Guid idExecutionPointer);
}