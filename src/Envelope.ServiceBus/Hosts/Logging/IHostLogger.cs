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
		ITransactionManager? transactionManager = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	IErrorMessage LogError(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	void LogResultErrorMessages(
		IResult result,
		ITransactionManager? transactionManager = null);

	void LogResultAllMessages(
		IResult result,
		ITransactionManager? transactionManager = null);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task LogResultErrorMessagesAsync(
		IResult result,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task LogResultAllMessagesAsync(
		IResult result,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);
}
