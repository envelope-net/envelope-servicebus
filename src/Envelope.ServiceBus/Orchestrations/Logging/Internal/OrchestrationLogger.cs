using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.Orchestrations.Logging.Internal;

internal class OrchestrationLogger : IOrchestrationLogger
{
	private readonly ILogger _logger;

	public OrchestrationLogger(ILogger<OrchestrationLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder<Guid>> AppendToBuilder(
		Action<LogMessageBuilder<Guid>> messageBuilder,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		string? detail)
	{
		if (idOrchestration.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idOrchestration), idOrchestration.Value.ToString());

		if (idStep.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idStep), idStep.Value.ToString());

		if (idExecutionPointer.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idExecutionPointer), idExecutionPointer.Value.ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder<Guid>> AppendToBuilder(
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		string? detail)
	{
		if (idOrchestration.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idOrchestration), idOrchestration.Value.ToString());

		if (idStep.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idStep), idStep.Value.ToString());

		if (idExecutionPointer.HasValue)
			messageBuilder += x => x.AddCustomData(nameof(idExecutionPointer), idExecutionPointer.Value.ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	public Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		Guid? idOrchestration,
		Guid? idStep,
		Guid? idExecutionPointer,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, idOrchestration, idStep, idExecutionPointer, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}
}
