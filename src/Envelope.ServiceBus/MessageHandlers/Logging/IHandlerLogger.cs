using Envelope.Logging;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.MessageHandlers.Logging;

public interface IHandlerLogger
{
	ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogError(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);
}
