using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.EventHandlers;

public interface IOrchestrationEventQueue
{
	Task<IResult<Guid>> SaveNewEventAsync(OrchestrationEvent @event, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);

	Task<IResult<List<OrchestrationEvent>?, Guid>> GetUnprocessedEventsAsync(string orchestrationKey, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);

	Task<IResult<Guid>> UpdateEventAsync(OrchestrationEvent @event, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken);
}
