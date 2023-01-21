using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IStepLifeCycleEvent : ILifeCycleEvent, IEvent
{
	IExecutionPointer ExecutionPointer { get; }
}
