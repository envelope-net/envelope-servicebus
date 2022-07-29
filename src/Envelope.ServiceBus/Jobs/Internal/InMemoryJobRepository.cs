using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs.Internal;

internal class InMemoryJobRepository : IJobRepository
{
	public Task<TData?> LoadDataAsync<TData>(string jobName, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
		=> Task.FromResult((TData?)default);

	public Task SaveDataAsync<TData>(string jobName, TData data, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}
