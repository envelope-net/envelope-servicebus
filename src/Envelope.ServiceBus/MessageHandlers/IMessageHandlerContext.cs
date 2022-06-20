using Envelope.Logging;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlerContext : IMessageMetadata
{
	IServiceBusOptions ServiceBusOptions { get; }

	bool ThrowNoHandlerException { get; }

	/// <summary>
	/// Current transaction context
	/// </summary>
	ITransactionContext TransactionContext { get; }

	/// <summary>
	/// MessageBus's service provider
	/// </summary>
	IServiceProvider? ServiceProvider { get; }

	IMessageHandlerResultFactory MessageHandlerResultFactory { get; }

	/// <summary>
	/// ServiceBus Host
	/// </summary>
	IHostInfo HostInfo { get; }

	ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	IErrorMessage? LogError(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	IErrorMessage? LogCritical(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage?> LogErrorAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage?> LogCriticalAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionManager? transactionManager = null,
		CancellationToken cancellationToken = default);
}
