using Envelope.Logging;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs.Logging;

public interface IJobLogger
{
	void LogStatus(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus);

	Task LogStatusAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		CancellationToken cancellationToken = default);

	Task LogExecutionStartAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		DateTime startedUtc,
		bool finished = false);

	Task LogExecutionFinishedAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		DateTime startedUtc);

	Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<LogMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		bool force = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogErrorAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<ErrorMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<IErrorMessage> LogCriticalAsync(
		ITraceInfo traceInfo,
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		Action<ErrorMessageBuilder> messageBuilder,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task LogResultErrorMessagesAsync(
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		IResult result,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);

	Task LogResultAllMessagesAsync(
		IJob job,
		JobExecuteResult executeResult,
		JobExecuteStatus? newExecuteStatus,
		string logCode,
		IResult result,
		Guid? jobMessageId,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default);
}
