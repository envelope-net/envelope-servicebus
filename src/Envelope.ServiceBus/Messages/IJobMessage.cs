using Envelope.Trace;

namespace Envelope.ServiceBus.Messages;

public interface IJobMessage
{
	Guid Id { get; }
	int JobMessageTypeId { get; }
	DateTime CreatedUtc { get; }
	DateTime LastUpdatedUtc { get; }
	ITraceInfo CreatedTraceInfo { get; }
	ITraceInfo LastUpdatedTraceInfo { get; }
	Guid TaskCorrelationId { get; }
	int Priority { get; }
	DateTime? TimeToLive { get; }
	DateTime? DeletedUtc { get; }
	int RetryCount { get; }
	DateTime? DelayedToUtc { get; }
	TimeSpan? LastDelay { get; }
	int Status { get; }
	DateTime? LastResumedUtc { get; }

	string? EntityName { get; }
	Guid? EntityId { get; }
	Dictionary<string, object?>? Properties { get; }
	string? Detail { get; }
	bool IsDetailJson { get; }

	IJobMessage Clone();

	object? GetProperty(string key, object? defaultValue = default);

	T? GetProperty<T>(string key, T? defaultValue = default);

	void CopyFrom(IJobMessage message);

	void Complete(
		ITraceInfo traceInfo,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null);

	void SetErrorRetry(
		ITraceInfo traceInfo,
		DateTime? delayedToUtc,
		TimeSpan? delay,
		int maxRetryCount,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null);

	void Suspend(
		ITraceInfo traceInfo,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null);

	void Resume(
		ITraceInfo traceInfo,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null);

	void Delete(
		ITraceInfo traceInfo,
		Dictionary<string, object?>? properties = null,
		string? detail = null,
		bool? isDetailJson = null);

	string ToJson();
}
