using Envelope.Logging;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

public interface IOrchestrationError : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	IErrorMessage<Guid> ErrorMessage { get; }
}
