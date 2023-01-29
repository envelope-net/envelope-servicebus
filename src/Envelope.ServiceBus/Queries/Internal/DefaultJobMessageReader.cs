using Envelope.ServiceBus.Messages;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Queries.Internal;

internal class DefaultJobMessageReader : IJobMessageReader
{
	public Task<List<IJobMessage>> GetActiveJobMessagesAsync(
		int jobMessageTypeId,
		int? status = null,
		bool includeDeleted = false,
		int page = 1,
		int pageSize = 20,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public Task<List<IJobMessage>> GetActiveJobMessagesToArchiveAsync(
		DateTime lastUpdatedBeforeUtc,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<IJobMessage>());

	public Task<int> GetNextActiveJobMessagesCountAsync(
		int jobMessageTypeId,
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(0);

	public Task<int> GetSusspendedActiveJobMessagesCountAsync(
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
		ITransactionController? transactionController = null,
		CancellationToken cancellationToken = default)
		=> Task.FromResult((IJobMessage?)null);

	public void Dispose()
	{
	}

	public ValueTask DisposeAsync()
		=> ValueTask.CompletedTask;
}
