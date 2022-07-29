using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Configuration;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Execution.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.Dto;

public class OrchestrationInstanceDto
{
	public Guid IdOrchestrationInstance { get; set; }

	public string OrchestrationKey { get; set; }

	public Guid IdOrchestrationDefinition { get; set; }

	public int Version { get; set; }

	public OrchestrationStatus Status { get; set; }

	public object Data { get; set; }

	public DateTime CreateTimeUtc { get; set; }

	public DateTime? CompleteTimeUtc { get; set; }

	public TimeSpan WorkerIdleTimeout { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public OrchestrationInstanceDto()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public OrchestrationInstanceDto(IOrchestrationInstance orchestrationInstance)
	{
		if (orchestrationInstance == null)
			throw new ArgumentNullException(nameof(orchestrationInstance));

		IdOrchestrationInstance = orchestrationInstance.IdOrchestrationInstance;
		OrchestrationKey = orchestrationInstance.OrchestrationKey;
		//DistributedLockResourceType = orchestrationInstance.DistributedLockResourceType;
		IdOrchestrationDefinition = orchestrationInstance.IdOrchestrationDefinition;
		Version = orchestrationInstance.Version;
		Status = orchestrationInstance.Status;
		Data = orchestrationInstance.Data;
		CreateTimeUtc = orchestrationInstance.CreateTimeUtc;
		CompleteTimeUtc = orchestrationInstance.CompleteTimeUtc;
		WorkerIdleTimeout = orchestrationInstance.WorkerIdleTimeout;
	}

	public OrchestrationInstance ToOrchestrationInstance(
		IOrchestrationDefinition orchestrationDefinition,
		IServiceProvider serviceProvider,
		IHostInfo hostInfo)
	{
		if (serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		if (hostInfo == null)
			throw new ArgumentNullException(nameof(hostInfo));

		var orchestrationInstance = new OrchestrationInstance(
			IdOrchestrationInstance,
			orchestrationDefinition,
			OrchestrationKey,
			Data,
			OrchestrationExecutorManager.GetOrCreateOrchestrationExecutor(IdOrchestrationInstance, serviceProvider, serviceProvider.GetRequiredService<IOrchestrationHostOptions>()),
			hostInfo,
			WorkerIdleTimeout)
		{
			OrchestrationKey = OrchestrationKey,
			Status = Status,
			CreateTimeUtc = CreateTimeUtc,
			CompleteTimeUtc = CompleteTimeUtc,
			WorkerIdleTimeout = WorkerIdleTimeout,
		};

		return orchestrationInstance;
	}
}
