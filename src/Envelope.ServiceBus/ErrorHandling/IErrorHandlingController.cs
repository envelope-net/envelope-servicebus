namespace Envelope.ServiceBus.ErrorHandling;

public interface IErrorHandlingController
{
	IReadOnlyDictionary<int, TimeSpan> IterationRetryTable { get; } //Dictionary<IterationCount, TimeSpan>

	TimeSpan? DefaultRetryInterval { get; }

	int? MaxRetryCount { get; }

	bool Add(int iterationCount, TimeSpan delay, bool force = true);

	bool CanRetry(int currentRetryCount);

	TimeSpan? GetFirstRetryTimeSpan();

	TimeSpan? GetRetryTimeSpan(int currentRetryCount);
}
