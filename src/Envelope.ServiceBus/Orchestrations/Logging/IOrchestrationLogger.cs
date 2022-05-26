using Envelope.Logging;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Orchestrations.Logging;

public interface IOrchestrationLogger
{
	Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage<Guid>> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default);
}
