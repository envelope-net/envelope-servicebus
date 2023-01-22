using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IOrchestrationSuspended : ILifeCycleEvent, IEvent
{
	SuspendSource SuspendSource { get; }
}

public enum SuspendSource
{
	ByController = 1,
	ByExecutor = 2
}