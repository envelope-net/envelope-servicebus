using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.ServiceBus.Messages;
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
		IMessageMetadata? messageMetadata,
		string? detail)
	{
		if (messageMetadata != null)
		{
			messageBuilder += x => x
				.AddCustomData(nameof(messageMetadata.MessageId), messageMetadata.MessageId.ToString())
				.AddCustomData(nameof(messageMetadata.MessageStatus), ((int)messageMetadata.MessageStatus).ToString());
		}

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder> AppendToBuilder(
		Action<ErrorMessageBuilder> messageBuilder,
		IMessageMetadata? messageMetadata,
		string? detail)
	{
		if (messageMetadata != null)
		{
			messageBuilder += x => x
				.AddCustomData(nameof(messageMetadata.MessageId), messageMetadata.MessageId.ToString())
				.AddCustomData(nameof(messageMetadata.MessageStatus), ((int)messageMetadata.MessageStatus).ToString());
		}

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	public ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogError(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}
}
