using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs.Internal;

internal class InMemoryJobRepository : IJobRepository
{
	public Task<TData?> LoadDataAsync<TData>(string jobName, ITransactionController transactionController, CancellationToken cancellationToken = default)
		=> Task.FromResult((TData?)default);

	public Task SaveDataAsync<TData>(string jobName, TData? data, ITransactionController transactionController, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}
