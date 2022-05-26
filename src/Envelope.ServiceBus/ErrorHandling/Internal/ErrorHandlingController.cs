namespace Envelope.ServiceBus.ErrorHandling.Internal;

internal class ErrorHandlingController : IErrorHandlingController
{
	public Dictionary<int, TimeSpan> IterationRetryTable { get; set; } //Dictionary<IterationCount, TimeSpan>
	IReadOnlyDictionary<int, TimeSpan> IErrorHandlingController.IterationRetryTable => IterationRetryTable;

	public TimeSpan? DefaultRetryInterval { get; set; }

	public int? MaxRetryCount { get; set; }

	internal ErrorHandlingController()
	{
		DefaultRetryInterval = TimeSpan.FromSeconds(300);
		IterationRetryTable = new Dictionary<int, TimeSpan>();
		MaxRetryCount = null;
	}

	public ErrorHandlingController(Dictionary<int, TimeSpan> delayTable)
	{
		if (delayTable == null)
			throw new ArgumentNullException(nameof(delayTable));

		IterationRetryTable = new Dictionary<int, TimeSpan>(delayTable);
	}

	public bool Add(int iterationCount, TimeSpan delay, bool force = true)
	{
		if (iterationCount < 0)
			throw new ArgumentOutOfRangeException(nameof(iterationCount));

		if (force)
		{
			IterationRetryTable[iterationCount] = delay;
			return true;
		}
		else
		{
			var result = IterationRetryTable.TryAdd(iterationCount, delay);
			return result;
		}
	}

	public bool CanRetry(int currentRetryCount)
		=> !MaxRetryCount.HasValue || currentRetryCount < MaxRetryCount;

	public TimeSpan? GetFirstRetryTimeSpan()
	{
		if (MaxRetryCount == 0)
			return null;

		if (IterationRetryTable.Count == 0)
			return DefaultRetryInterval;

		var minIteration = IterationRetryTable.Keys.Where(x => 0 <= x).Min();
		return IterationRetryTable[minIteration];
	}

	public TimeSpan? GetRetryTimeSpan(int currentRetryCount)
	{
		if (MaxRetryCount < currentRetryCount)
			return null;

		if (IterationRetryTable.Count == 0)
			return DefaultRetryInterval;

		TimeSpan? result = null;
		int? bestDelta = null;
		foreach (var retry in IterationRetryTable.Keys.Where(x => 0 <= x))
		{
			var value = IterationRetryTable[retry];
			var delta = Math.Abs(retry - currentRetryCount);
			if (bestDelta.HasValue)
			{
				if ((delta < bestDelta.Value)
					|| (delta == bestDelta.Value && value < result))
				{
					bestDelta = delta;
					result = value;
				}
			}
			else
			{
				bestDelta = delta;
				result = value;
			}
		}

		return result ?? DefaultRetryInterval;
	}
}
