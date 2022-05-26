using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Services;
using Envelope.Services.Extensions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.Hosts.Logging;

public class HostLogger : IHostLogger
{
	private readonly ILogger _logger;

	public HostLogger(ILogger<HostLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder<Guid>> AppendToBuilder(
		Action<LogMessageBuilder<Guid>> messageBuilder,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(hostInfo.HostId), hostInfo.HostId.ToString())
			.AddCustomData(nameof(hostInfo.HostName), hostInfo.HostName.ToString())
			.AddCustomData(nameof(hostStatus), ((int)hostStatus).ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder<Guid>> AppendToBuilder(
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(hostInfo.HostId), hostInfo.HostId.ToString())
			.AddCustomData(nameof(hostInfo.HostName), hostInfo.HostName.ToString())
			.AddCustomData(nameof(hostStatus), ((int)hostStatus).ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	public ILogMessage<Guid>? LogTrace(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogDebug(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogInformation(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage<Guid>? LogWarning(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage<Guid> LogError(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage<Guid> LogCritical(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public void LogResultErrorMessages(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null)
		=> _logger.LogResultErrorMessages(result, true);

	public void LogResultAllMessages(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null)
		=> _logger.LogResultAllMessages(result, true);

	public Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage<Guid>> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		IHostInfo hostInfo,
		HostStatus hostStatus,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, hostStatus, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task LogResultErrorMessagesAsync(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogResultErrorMessages(result, true);
		return Task.CompletedTask;
	}

	public Task LogResultAllMessagesAsync(
		IResult<Guid> result,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogResultAllMessages(result, true);
		return Task.CompletedTask;
	}
}
