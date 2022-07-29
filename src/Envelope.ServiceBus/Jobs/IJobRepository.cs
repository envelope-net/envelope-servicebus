using Envelope.ServiceBus.Orchestrations.EventHandlers;
using Envelope.Transactions;

namespace Envelope.ServiceBus.Jobs;

public interface IJobRepository
{
	Task<TData?> LoadDataAsync<TData>(
		string jobName,
		ITransactionContext transactionContext,
		CancellationToken cancellationToken = default);

	Task SaveDataAsync<TData>(
		string jobName,
		TData data,
		ITransactionContext transactionContext,
		CancellationToken cancellationToken = default);
}
