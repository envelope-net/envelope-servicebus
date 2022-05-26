using Envelope.ServiceBus.Orchestrations.Definition;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public interface IOrchestrationRegistry
{
	void RegisterOrchestration(IOrchestrationDefinition orchestrationDefinition);

	void RegisterOrchestration<TData>(IOrchestration<TData> orchestration);

	IOrchestrationDefinition? GetDefinition(Guid idOrchestrationDefinition, int? version = null);

	bool IsRegistered(Guid idOrchestrationDefinition, int version);

	IEnumerable<IOrchestrationDefinition> GetAllDefinitions();
}
