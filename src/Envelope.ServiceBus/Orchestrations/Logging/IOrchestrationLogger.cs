using Envelope.Logging;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Orchestrations.Logging;

public interface IOrchestrationLogger
{
	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);
}
