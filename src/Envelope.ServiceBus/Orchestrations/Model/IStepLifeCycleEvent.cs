using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IStepLifeCycleEvent : ILifeCycleEvent, IEvent
{
	IExecutionPointer ExecutionPointer { get; }
}
