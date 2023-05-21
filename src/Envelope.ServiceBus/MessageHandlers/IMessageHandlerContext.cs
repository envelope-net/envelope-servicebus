using Envelope.Logging;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlerContext
{
	/// <summary>
	/// MessageBus's service provider
	/// </summary>
	IServiceProvider? ServiceProvider { get; }

	/// <summary>
	/// ServiceBus Host
	/// </summary>
	IHostInfo HostInfo { get; }

	/// <summary>
	/// Current transaction context
	/// </summary>
	ITransactionController TransactionController { get; }

	ITraceInfo TraceInfo { get; }

	IHandlerLogger HandlerLogger { get; }

	void Initialize(
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		IHandlerLogger handlerLogger);

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

	IErrorMessage? LogError(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null);

	IErrorMessage? LogCritical(
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

	Task<IErrorMessage?> LogErrorAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage?> LogCriticalAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);
}
