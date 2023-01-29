using Envelope.ServiceBus.Messages;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queries;

public interface IJobMessageReader : IDisposable, IAsyncDisposable
{
	Task<List<IJobMessage>> GetActiveJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		int page = 1,
		int pageSize = 20,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<List<IJobMessage>> GetActiveJobMessagesToArchiveAsync(
		DateTime lastUpdatedBeforeUtc,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetNextActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);

	Task<int> GetSusspendedActiveJobMessagesCountAsync(
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
	/// <param name="transactionController"></param>
	/// <param name="cancellationToken"></param>
	Task<IJobMessage?> GetNextActiveJobMessageAsync(
		int jobMessageTypeId,
		DateTime? maxDelayedToUtc,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default);
}
