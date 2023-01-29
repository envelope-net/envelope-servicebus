using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Writers;

public interface IJobMessagePublisher
{
	Task PublishJobMessageAsync(
		ITraceInfo traceInfo,
		int jobMessageTypeId,
		ITransactionController? transactionController,
		Guid? taskCorrelationId = null,
		int priority = 100,
		DateTime? timeToLive = null,
		string? entityName = null,
		Guid? entityId = null,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);
}
