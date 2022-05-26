using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.MessageHandlers.Logging;

internal class HandlerLogger : IHandlerLogger
{
	private readonly ILogger _logger;

	public HandlerLogger(ILogger<HandlerLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder<Guid>> AppendToBuilder(
		Action<LogMessageBuilder<Guid>> messageBuilder,
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

	private static Action<ErrorMessageBuilder<Guid>> AppendToBuilder(
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
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

	public ILogMessage<Guid>? LogTrace(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogDebug(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogInformation(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogWarning(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage<Guid> LogError(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage<Guid> LogCritical(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, messageMetadata, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}
}
