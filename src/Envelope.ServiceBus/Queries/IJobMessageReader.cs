using Envelope.ServiceBus.Messages;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queries;

public interface IJobMessageReader : IDisposable, IAsyncDisposable
{
	Task<IJobMessage?> GetActiveJobMessageAsync(
		Guid jobMessageId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<IJobMessage?> GetArchivedJobMessageAsync(
		Guid jobMessageId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetActiveJobMessagesAsync(
		List<Guid> jobMessageIds,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetArchivedJobMessagesAsync(
		List<Guid> jobMessageIds,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetActiveJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		int page = 1,
		int pageSize = 20,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetActiveJobMessagesToArchiveAsync(
		DateTime lastUpdatedBeforeUtc,
		bool includeSuspended = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetIdleActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetCompletedActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetErrorActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetSuspendedActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetAllActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetArchivedJobMessagesCountAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetArchivedJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		int page = 1,
		int pageSize = 20,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	/// <param name="jobMessageTypeId"></param>
	/// <param name="maxDelayedToUtc">EXPECTED: DateTime.UtcNow OR null</param>
	/// <param name="skipSuspendedMessages"></param>
	/// <param name="transactionController"></param>
	/// <param name="cancellationToken"></param>
	Task<IJobMessage?> GetNextActiveJobMessageAsync(
		int jobMessageTypeId,
		DateTime? maxDelayedToUtc,
		bool skipSuspendedMessages = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetActiveEntityJobMessagesAsync(
		string entityName,
		Guid? entityId,
		int? jobMessageTypeId,
		int? status = null,
		int page = 1,
		int pageSize = 20,
		bool includeDeleted = false,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);
}
