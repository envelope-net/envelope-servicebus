using Envelope.Logging;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Orchestrations.Model;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IOrchestrationError : IStepLifeCycleEvent, ILifeCycleEvent, IEvent
{
	IErrorMessage ErrorMessage { get; }
}
