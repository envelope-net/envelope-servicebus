using Envelope.Logging;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlerContext : IMessageMetadata
{
	bool ThrowNoHandlerException { get; }

	/// <summary>
	/// Current transaction context
	/// </summary>
	ITransactionContext? TransactionContext { get; }

	/// <summary>
	/// MessageBus's service provider
	/// </summary>
	IServiceProvider? ServiceProvider { get; }

	IMessageHandlerResultFactory MessageHandlerResultFactory { get; }

	/// <summary>
	/// ServiceBus Host
	/// </summary>
	IHostInfo HostInfo { get; }

	ILogMessage<Guid>? LogTrace(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogDebug(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogInformation(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	ILogMessage<Guid>? LogWarning(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage<Guid>? LogError(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	IErrorMessage<Guid>? LogCritical(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null);

	Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>?> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>?> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);
}
