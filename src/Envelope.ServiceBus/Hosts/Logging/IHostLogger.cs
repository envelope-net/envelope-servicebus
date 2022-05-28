using Envelope.Logging;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Hosts.Logging;

public interface IHostLogger
{
	ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage LogError(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	void LogResultErrorMessages(
		IResult result,
		ITransactionContext? transactionContext = null);

	void LogResultAllMessages(
		IResult result,
		ITransactionContext? transactionContext = null);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task LogResultErrorMessagesAsync(
		IResult result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task LogResultAllMessagesAsync(
		IResult result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);
}
