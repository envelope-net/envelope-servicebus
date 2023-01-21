using Envelope.Infrastructure;
using Envelope.Logging;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Hosts.Logging;

public interface IHostLogger
{
	void LogStatusHost(
		ITraceInfo traceInfo,
		IHostInfo hostInfo);

	ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogError(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	void LogResultErrorMessages(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null);

	void LogResultAllMessages(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null);

	Task LogStatusHostAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task LogResultErrorMessagesAsync(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task LogResultAllMessagesAsync(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	void LogEnvironmentInfo(EnvironmentInfo environmentInfo);
}
