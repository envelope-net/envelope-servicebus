using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Writers;

public interface IJobMessageWriter : IJobMessagePublisher
{
	Task WriteActiveJobMessageAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		CancellationToken cancellationToken = default);

	Task WriteCompleteAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);

	Task WriteSetErrorRetryAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		DateTime? delayedToUtc,
		TimeSpan? delay,
		int maxRetryCount,
		ITransactionController? transactionController,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);

	Task WriteSusspendAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);

	Task WriteResumeAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);

	Task WriteDeleteAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null,
		CancellationToken cancellationToken = default);

	Task WriteArchiveJobMessageAsync(
		IJobMessage message,
		ITraceInfo traceInfo,
		ITransactionController? transactionController,
		CancellationToken cancellationToken = default);
}
