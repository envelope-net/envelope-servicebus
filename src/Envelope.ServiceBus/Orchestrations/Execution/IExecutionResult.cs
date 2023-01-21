namespace Envelope.ServiceBus.Orchestrations.Execution;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IExecutionResult
{
	IReadOnlyList<Guid>? NextSteps { get; }

	IReadOnlyList<Guid>? NestedSteps { get; }

	bool IsError { get; }

	bool Retry { get; }

	TimeSpan? RetryInterval { get; }

	string? EventName { get; }

	string? EventKey { get; }

	DateTime? EventWaitingTimeToLiveUtc { get; }
}
