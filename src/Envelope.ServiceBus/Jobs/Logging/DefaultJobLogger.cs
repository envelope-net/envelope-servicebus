using Envelope.Logging;
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
		string logCode,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
			.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
			.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
			.AddCustomData(nameof(job.Name), job?.Name);

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder += x => x.AddCustomData(nameof(detail), detail);

		if (!string.IsNullOrWhiteSpace(logCode))
			messageBuilder += x => x.LogCode(logCode, force: false);

		executeResult.SetStatus(newExecuteStatus);

		return messageBuilder;
	}

	private static Action<ErrorMessageBuilder> AppendToBuilder(
		Action<ErrorMessageBuilder> messageBuilder,
		string logCode,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string? detail)
	{
		messageBuilder += x => x
			.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
			.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
			.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
			.AddCustomData(nameof(job.Name), job?.Name);

		if (!string.IsNullOrWhiteSpace(detail))
			messageBuilder += x => x.AddCustomData(nameof(detail), detail);

		if (!string.IsNullOrWhiteSpace(logCode))
			messageBuilder += x => x.LogCode(logCode, force: false);

		executeResult.SetStatus(newExecuteStatus);

		return messageBuilder;
	}

	public void LogStatus(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		executeResult.SetStatus(newExecuteStatus);
	}

	public Task LogStatusAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		executeResult.SetStatus(newExecuteStatus);
		return Task.CompletedTask;
	}

	public Task LogExecutionStartAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		DateTime startedUtc,
		bool finished = false)
		=> Task.CompletedTask;

	public Task LogExecutionFinishedAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		DateTime startedUtc)
		=> Task.CompletedTask;

	public Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogTraceMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogDebugMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogInformationMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogWarningMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogErrorMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionController? transactionController= null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		AppendToBuilder(messageBuilder, logCode, job, executeResult, newExecuteStatus, detail);
		var msg = _logger.LogCriticalMessage(traceInfo, messageBuilder, true);
		return Task.FromResult(msg);
	}

	public Task LogResultErrorMessagesAsync(
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		if (result == null)
			throw new ArgumentNullException(nameof(result));

		foreach (var msg in result.ErrorMessages)
		{
			var builder = new ErrorMessageBuilder(msg);
			builder
				.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
				.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
				.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
				.AddCustomData(nameof(job.Name), job.Name)
				.LogCode(logCode, force: true);
		}

		executeResult.SetStatus(newExecuteStatus);

		_logger.LogResultErrorMessages(result, true);
		return Task.CompletedTask;
	}

	public Task LogResultAllMessagesAsync(
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		IResult result,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		if (executeResult == null)
			throw new ArgumentNullException(nameof(executeResult));

		if (result == null)
			throw new ArgumentNullException(nameof(result));

		foreach (var msg in result.SuccessMessages)
		{
			var builder = new LogMessageBuilder(msg);
			builder
				.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
				.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
				.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
				.AddCustomData(nameof(job.Name), job.Name)
				.LogCode(logCode, force: true);
		}

		foreach (var msg in result.WarningMessages)
		{
			var builder = new LogMessageBuilder(msg);
			builder
				.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
				.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
				.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
				.AddCustomData(nameof(job.Name), job.Name)
				.LogCode(logCode, force: true);
		}

		foreach (var msg in result.ErrorMessages)
		{
			var builder = new ErrorMessageBuilder(msg);
			builder
				.AddCustomData(nameof(executeResult.ExecutionId), executeResult.ExecutionId.ToString())
				.AddCustomData(nameof(executeResult.ExecuteStatus), executeResult.ExecuteStatus.ToString())
				.AddCustomData(nameof(newExecuteStatus), newExecuteStatus?.ToString())
				.AddCustomData(nameof(job.Name), job.Name)
				.LogCode(logCode, force: true);
		}

		executeResult.SetStatus(newExecuteStatus);

		_logger.LogResultAllMessages(result, true);
		return Task.CompletedTask;
	}
}
