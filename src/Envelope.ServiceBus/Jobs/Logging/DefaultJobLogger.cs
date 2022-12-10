﻿using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;

namespace Envelope.ServiceBus.Jobs.Logging;

public class DefaultJobLogger : IJobLogger
{
	private readonly ILogger _logger;

	public DefaultJobLogger(ILogger<DefaultJobLogger> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	private static Action<LogMessageBuilder> AppendToBuilder(
		Action<LogMessageBuilder> messageBuilder,
		string jobName,
		string? detail)
	{
		if (!string.IsNullOrWhiteSpace(jobName))
			messageBuilder += x => x.AddCustomData(nameof(jobName), jobName);

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder> AppendToBuilder(
		Action<ErrorMessageBuilder> messageBuilder,
		string jobName,
		string? detail)
	{
		if (!string.IsNullOrWhiteSpace(jobName))
			messageBuilder += x => x.AddCustomData(nameof(jobName), jobName);

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder +=
				x => x.AddCustomData(nameof(detail), detail);

		return messageBuilder;
	}

	public Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		string jobName,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		AppendToBuilder(messageBuilder, jobName, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task LogResultErrorMessagesAsync(
		string jobName,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		foreach (var msg in result.ErrorMessages)
		{
			var builder = new ErrorMessageBuilder(msg);
			builder.AppendDetail($"{nameof(jobName)} = {jobName}");
		}

		_logger.LogResultErrorMessages(result, true);
		return Task.CompletedTask;
	}

	public Task LogResultAllMessagesAsync(
		string jobName,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		foreach (var msg in result.SuccessMessages)
		{
			var builder = new LogMessageBuilder(msg);
			builder.AppendDetail($"{nameof(jobName)} = {jobName}");
		}

		foreach (var msg in result.WarningMessages)
		{
			var builder = new LogMessageBuilder(msg);
			builder.AppendDetail($"{nameof(jobName)} = {jobName}");
		}

		foreach (var msg in result.ErrorMessages)
		{
			var builder = new ErrorMessageBuilder(msg);
			builder.AppendDetail($"{nameof(jobName)} = {jobName}");
		}

		_logger.LogResultAllMessages(result, true);
		return Task.CompletedTask;
	}
}
