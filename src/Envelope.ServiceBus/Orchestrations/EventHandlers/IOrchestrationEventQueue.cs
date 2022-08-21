using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Orchestrations.EventHandlers;

public interface IOrchestrationEventQueue
{
	Task<IResult> SaveNewEventAsync(OrchestrationEvent @event, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken);

	Task<IResult<List<OrchestrationEvent>?>> GetUnprocessedEventsAsync(string orchestrationKey, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken);

	Task<IResult> SetProcessedUtcAsync(OrchestrationEvent @event, ITraceInfo traceInfo, ITransactionController transactionController, CancellationToken cancellationToken);
}
