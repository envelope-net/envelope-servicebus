namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IReadOnlyExecutionPointerCollection : IReadOnlyCollection<ExecutionPointer>
{
	ExecutionPointer? FindById(Guid idExecutionPointer);
}