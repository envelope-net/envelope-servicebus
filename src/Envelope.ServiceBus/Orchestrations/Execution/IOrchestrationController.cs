using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public interface IOrchestrationController
{
	void RegisterOrchestration<TOrchestration, TData>()
		where TOrchestration : IOrchestration<TData>;

	event OrchestrationEventHandler OnLifeCycleEvent;

	/// <returns>returns instance id</returns>
	Task<IResult<Guid, Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		TData data,
		string lockOwner,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? workerIdleTimeout = null);

	/// <returns>returns instance id</returns>
	Task<IResult<Guid, Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		int? version,
		TData data,
		string lockOwner,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? workerIdleTimeout = null);

	Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance);

	/// <summary>
	/// Suspend the execution of a given orchestration until <see cref="ResumeOrchestrationAsync(Guid, string, ITraceInfo{Guid})"/> is called
	/// </summary>
	Task<IResult<bool, Guid>> SuspendOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo);

	/// <summary>
	/// Resume a previously suspended orchestration
	/// </summary>
	Task<IResult<bool, Guid>> ResumeOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo);

	/// <summary>
	/// Permanently terminate the exeuction of a given orchestration
	/// </summary>
	Task<IResult<bool, Guid>> TerminateOrchestrationAsync(Guid idOrchestrationInstance, string lockOwner, ITraceInfo<Guid> traceInfo);

	internal Task PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo<Guid> traceInfo);
}


public delegate Task OrchestrationEventHandler(LifeCycleEvent lifeCycleEvent, ITraceInfo<Guid> traceInfo);