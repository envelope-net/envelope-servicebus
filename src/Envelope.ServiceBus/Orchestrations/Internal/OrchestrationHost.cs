using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.Hosting;

namespace Envelope.ServiceBus.Orchestrations.Internal;

internal class OrchestrationHost : BackgroundService, IOrchestrationHost, IDisposable
{
	private readonly IOrchestrationController _orchestrationController;

	public IHostInfo HostInfo { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public OrchestrationHost(IOrchestrationController orchestrationController)
	{
		_orchestrationController = orchestrationController ?? throw new ArgumentNullException(nameof(orchestrationController));
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public event OrchestrationEventHandler OnLifeCycleEvent
	{
		add
		{
			_orchestrationController.OnLifeCycleEvent += value;
		}

		remove
		{
			_orchestrationController.OnLifeCycleEvent -= value;
		}
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		//TODO
		throw new NotImplementedException();
	}

	public Task<IResult<Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		TData data,
		string lockOwner,
		ITraceInfo traceInfo,
		TimeSpan? workerIdleTimeout = null)
		=> _orchestrationController.StartOrchestrationAsync(
			idOrchestrationDefinition,
			orchestrationKey,
			null,
			data,
			lockOwner,
			traceInfo,
			workerIdleTimeout);

	public Task<IResult<Guid>> StartOrchestrationAsync<TData>(
		Guid idOrchestrationDefinition,
		string orchestrationKey,
		int? version,
		TData data,
		string lockOwner,
		ITraceInfo traceInfo,
		TimeSpan? workerIdleTimeout = null)
		=> _orchestrationController.StartOrchestrationAsync(
			idOrchestrationDefinition,
			orchestrationKey,
			version,
			data,
			lockOwner,
			traceInfo,
			workerIdleTimeout);

	public void RegisterOrchestration<TOrchestration, TData>()
		where TOrchestration : IOrchestration<TData>
		=> _orchestrationController.RegisterOrchestration<TOrchestration, TData>();

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance)
		=> _orchestrationController.GetOrchestrationInstanceAsync(idOrchestrationInstance);

	public Task<IResult<bool>> SuspendOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.SuspendOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	public Task<IResult<bool>> ResumeOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.ResumeOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	public Task<IResult<bool>> TerminateOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.TerminateOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	Task IOrchestrationController.PublishLifeCycleEventAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo traceInfo)
		=> _orchestrationController.PublishLifeCycleEventAsync(lifeCycleEvent, traceInfo);
}
