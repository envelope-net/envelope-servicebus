using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Execution;
using Envelope.ServiceBus.Orchestrations.Execution.Internal;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Hosting;

namespace Envelope.ServiceBus.Orchestrations.Internal;

internal class OrchestrationHost : BackgroundService, IOrchestrationHost, IDisposable
{
	private readonly IOrchestrationController _orchestrationController;

	public IOrchestrationController OrchestrationControllerInternal => _orchestrationController;

	public IHostInfo HostInfo { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public OrchestrationHost(IServiceProvider serviceProvider, IOrchestrationHostOptions options)
	{
		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		if (options == null)
			throw new ArgumentNullException(nameof(options));

		_orchestrationController = new OrchestrationController(serviceProvider, options);
		HostInfo = new HostInfo(options.HostName);
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

	public IOrchestrationDefinition RegisterOrchestration<TOrchestration, TData>(ITraceInfo traceInfo)
		where TOrchestration : IOrchestration<TData>
		=> _orchestrationController.RegisterOrchestration<TOrchestration, TData>(traceInfo);

	public Task<IOrchestrationInstance?> GetOrchestrationInstanceAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
		=> _orchestrationController.GetOrchestrationInstanceAsync(idOrchestrationInstance, cancellationToken);

	public Task<List<IOrchestrationInstance>> GetAllUnfinishedOrchestrationInstancesAsync(Guid idOrchestrationDefinition, CancellationToken cancellationToken = default)
		=> _orchestrationController.GetAllUnfinishedOrchestrationInstancesAsync(idOrchestrationDefinition, cancellationToken);

	public Task<bool?> IsCompletedOrchestrationAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
		=> _orchestrationController.IsCompletedOrchestrationAsync(idOrchestrationInstance, cancellationToken);

	public Task<List<ExecutionPointer>> GetOrchestrationExecutionPointersAsync(Guid idOrchestrationInstance, CancellationToken cancellationToken = default)
		=> _orchestrationController.GetOrchestrationExecutionPointersAsync(idOrchestrationInstance, cancellationToken);

	public Task<IResult<bool>> SuspendOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.SuspendOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	public Task<IResult<bool>> ResumeOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.ResumeOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	public Task<IResult<bool>> TerminateOrchestrationAsync(Guid orchestrationId, string lockOwner, ITraceInfo traceInfo)
		=> _orchestrationController.TerminateOrchestrationAsync(orchestrationId, lockOwner, traceInfo);

	Task IOrchestrationController.PublishLifeCycleEventInternalAsync(LifeCycleEvent lifeCycleEvent, ITraceInfo traceInfo, ITransactionController transactionController)
		=> _orchestrationController.PublishLifeCycleEventInternalAsync(lifeCycleEvent, traceInfo, transactionController);
}
