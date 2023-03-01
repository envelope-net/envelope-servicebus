using Envelope.ServiceBus.Messages;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queries.Internal;

internal class DefaultServiceBusReader : IServiceBusReader
{
	public Task<List<IDbHost>> GetHostsAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbHost>());

	public Task<List<IDbHostLog>> GetHostLogsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbHostLog>());

	public Task<List<IDbJob>> GetJobsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJob>());

	public Task<List<IDbJob>> GetJobsAsync(string jobName, string hostName, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJob>());

	public Task<IDbJob?> GetJobAsync(Guid jobInstanceId, CancellationToken cancellationToken = default)
		=> Task.FromResult((IDbJob?)null);

	public Task<List<IDbJobExecution>> GetJobLatestExecutionsAsync(Guid jobInstanceId, int page = 1, int pageSize = 3, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJobExecution>());

	public Task<List<IDbJobExecution>> GetJobExecutionsAsync(Guid jobInstanceId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJobExecution>());

	public Task<IDbJobExecution?> GetJobExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
		=> Task.FromResult((IDbJobExecution?)null);

	public Task<List<IDbJobLog>> GetJobLogsAsync(Guid executionId, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJobLog>());

	public Task<List<IDbJobLog>> JobLogsForMessageAsync(Guid jobMessageId, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJobLog>());

	public Task<List<IDbJobLog>> JobLogsForCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IDbJobLog>());

	public Task<IDbJobLog?> GetJobLogAsync(Guid idLogMessage, CancellationToken cancellationToken = default)
		=> Task.FromResult((IDbJobLog?)null);

	public Task<IJobMessage?> GetActiveJobMessageAsync(
		Guid jobMessageId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult((IJobMessage?)null);

	public Task<IJobMessage?> GetArchivedJobMessageAsync(
		Guid jobMessageId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult((IJobMessage?)null);

	public Task<List<IJobMessage>> GetActiveJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		int page = 1,
		int pageSize = 20,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public Task<List<IJobMessage>> GetActiveJobMessagesToArchiveAsync(
		DateTime lastUpdatedBeforeUtc,
		bool includeSuspended = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public Task<int> GetIdleActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetCompletedActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetErrorActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetSuspendedActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetAllActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetArchivedJobMessagesCountAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<List<IJobMessage>> GetArchivedJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		int page = 1,
		int pageSize = 20,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public Task<IJobMessage?> GetNextActiveJobMessageAsync(
		int jobMessageTypeId,
		DateTime? maxDelayedToUtc,
		bool skipSuspendedMessages = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult((IJobMessage?)null);

	public Task<List<IJobMessage>> GetActiveEntityJobMessagesAsync(
		string entityName,
		Guid? entityId,
		int? jobMessageTypeId,
		int? status = null,
		int page = 1,
		int pageSize = 20,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public void Dispose()
	{
	}

	public ValueTask DisposeAsync()
		=> ValueTask.CompletedTask;
}
