using Envelope.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.MessageHandlers.Logging;

public interface IHandlerLogger
{
	ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogError(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);
}
