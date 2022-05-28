using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.EventHandlers;

public interface IOrchestrationEventQueue
{
	Task<IResult> SaveNewEventAsync(OrchestrationEvent @event, ITraceInfo traceInfo, CancellationToken cancellationToken);

	Task<IResult<List<OrchestrationEvent>?>> GetUnprocessedEventsAsync(string orchestrationKey, ITraceInfo traceInfo, CancellationToken cancellationToken);

	Task<IResult> UpdateEventAsync(OrchestrationEvent @event, ITraceInfo traceInfo, CancellationToken cancellationToken);
}
