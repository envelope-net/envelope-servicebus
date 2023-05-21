using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.MessageHandlers.Logging;

public class DefaultHandlerLogger : IHandlerLogger
{
	private readonly ILogger _logger;

	public DefaultHandlerLogger(ILogger<DefaultHandlerLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder> AppendToBuilder(
		Action<LogMessageBuilder> messageBuilder,
		string? detail)
	{
		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder> AppendToBuilder(
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail)
	{
		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	public ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogError(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		messageBuilder = AppendToBuilder(messageBuilder, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}
}
