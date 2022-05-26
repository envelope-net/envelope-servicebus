using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IOrchestrationSuspended : ILifeCycleEvent, IEvent
{
	SuspendSource SuspendSource { get; }
}

public enum SuspendSource
{
	ByController = 1,
	ByExecutor = 2
}