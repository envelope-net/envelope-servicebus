using Envelope.Infrastructure;
using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.Hosts.Logging;

public class DefaultHostLogger : IHostLogger
{
	private readonly ILogger _logger;

	public DefaultHostLogger(ILogger<DefaultHostLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder> AppendToBuilder(
		Action<LogMessageBuilder> messageBuilder,
		IHostInfo hostInfo,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(hostInfo.HostId), hostInfo.HostId.ToString())
			.AddCustomData(nameof(hostInfo.HostName), hostInfo.HostName.ToString())
			.AddCustomData(nameof(hostInfo.HostStatus), ((int)hostInfo.HostStatus).ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder> AppendToBuilder(
		Action<ErrorMessageBuilder> messageBuilder,
		IHostInfo hostInfo,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(hostInfo.HostId), hostInfo.HostId.ToString())
			.AddCustomData(nameof(hostInfo.HostName), hostInfo.HostName.ToString())
			.AddCustomData(nameof(hostInfo.HostStatus), ((int)hostInfo.HostStatus).ToString());

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}
	public void LogStatusHost(
		ITraceInfo traceInfo,
		IHostInfo hostInfo)
	{
	}

	public ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogError(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public IErrorMessage LogCritical(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return msg;
	}

	public void LogResultErrorMessages(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null)
		=> _logger.LogResultErrorMessages(result, true);

	public void LogResultAllMessages(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null)
		=> _logger.LogResultAllMessages(result, true);

	public Task LogStatusHostAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		CancellationToken cancellationToken = default)
		=> Task.CompletedTask;

	public Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, hostInfo, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task LogResultErrorMessagesAsync(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogResultErrorMessages(result, true);
		return Task.CompletedTask;
	}

	public Task LogResultAllMessagesAsync(
		IHostInfo hostInfo,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogResultAllMessages(result, true);
		return Task.CompletedTask;
	}

	public void LogEnvironmentInfo(EnvironmentInfo environmentInfo)
	{
	}
}
