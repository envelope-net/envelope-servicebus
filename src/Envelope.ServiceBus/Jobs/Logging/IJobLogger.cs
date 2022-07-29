using Envelope.Logging;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs.Logging;

public interface IJobLogger
{
	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);
}
