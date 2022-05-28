using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Model;
using Envelope.Trace;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IServiceBusLifeCycleEventManager
{
	event ServiceBusEventHandler OnServiceBusEvent;

	Task PublishServiceBusEventInternalAsync(IServiceBusEvent serviceBusEvent, ITraceInfo traceInfo, IServiceBusOptions serviceBusOptions);
}



public delegate Task ServiceBusEventHandler(IServiceBusEvent serviceBusEvent, ITraceInfo traceInfo);
