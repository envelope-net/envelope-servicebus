using Envelope.Logging;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Hosts.Logging;

public interface IHostLogger
{
	ILogMessage<Guid>? LogTrace(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogDebug(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogInformation(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogWarning(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage<Guid> LogError(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage<Guid> LogCritical(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	void LogResultErrorMessages(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null);

	void LogResultAllMessages(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null);

	Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task LogResultErrorMessagesAsync(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task LogResultAllMessagesAsync(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);
}
