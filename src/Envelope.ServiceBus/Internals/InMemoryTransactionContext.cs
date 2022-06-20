using Envelope.Transactions;

namespace Envelope.ServiceBus.Internals;

internal class InMemoryTransactionContext : ITransactionContext
{
	private readonly object _lock = new();

	public ITransactionManager TransactionManager { get; }
	public TransactionResult TransactionResult { get; private set; }
	public string? RollbackErrorInfo { get; private set; }

	public InMemoryTransactionContext(ITransactionManager transactionManager)
	{
		TransactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
	}

	public void ScheduleCommit()
	{
		lock (_lock)
		{
			if (TransactionResult != TransactionResult.Rollback)
				TransactionResult = TransactionResult.Commit;
		}
	}

	public void ScheduleRollback(string? rollbackErrorInfo = null)
	{
		lock (_lock)
		{
			TransactionResult = TransactionResult.Rollback;
			RollbackErrorInfo = rollbackErrorInfo;
		}
	}

	public void Dispose()
		=> TransactionManager.Dispose();

	public ValueTask DisposeAsync()
		=> TransactionManager.DisposeAsync();
}
