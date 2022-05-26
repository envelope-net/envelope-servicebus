using Envelope.ServiceBus.Orchestrations.Execution.Internal;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public static class ExecutionResultFactory
{
	public static IExecutionResult NextStep()
	{
		return new ExecutionResult
		{
			NextSteps = new List<Guid> { Constants.Guids.FULL }
		};
	}

	public static IExecutionResult Empty()
		=> new ExecutionResult();

	public static IExecutionResult BranchSteps(List<Guid> branchStepIds)
	{
		if (branchStepIds == null || branchStepIds.Count == 0)
			throw new ArgumentNullException(nameof(branchStepIds));

		return new ExecutionResult
		{
			BranchSteps = branchStepIds
		};
	}

	public static IExecutionResult RetryableError(TimeSpan? retryInterval = null)
	{
		return new ExecutionResult
		{
			IsError = true,
			Retry = true,
			RetryInterval = retryInterval
		};
	}

	public static IExecutionResult Delay(TimeSpan delayInterval)
	{
		return new ExecutionResult
		{
			IsError = false,
			Retry = true,
			RetryInterval = delayInterval
		};
	}

	public static IExecutionResult Suspend()
	{
		return new ExecutionResult
		{
			IsError = true,
			Retry = false
		};
	}

	public static IExecutionResult WaitForEvent(string eventName, string? eventKey, DateTime? timeToLiveUtc)
	{
		if (string.IsNullOrWhiteSpace(eventName))
			throw new ArgumentNullException(nameof(eventName));

		return new ExecutionResult
		{
			EventName = eventName,
			EventKey = eventKey,
			EventWaitingTimeToLiveUtc = timeToLiveUtc?.ToUniversalTime()
		};
	}
}
