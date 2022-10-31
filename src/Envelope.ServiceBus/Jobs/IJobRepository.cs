using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs;

public interface IJobRepository
{
	Task<TData?> LoadDataAsync<TData>(
		string jobName,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default);

	Task SaveDataAsync<TData>(
		string jobName,
		TData? data,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default);
}
