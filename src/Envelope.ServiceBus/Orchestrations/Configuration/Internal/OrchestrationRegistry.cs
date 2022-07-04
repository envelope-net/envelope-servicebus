using Envelope.Exceptions;
using Envelope.ServiceBus.Orchestrations.Definition;
using Envelope.ServiceBus.Orchestrations.Definition.Builder;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Orchestrations.Configuration.Internal;

internal class OrchestrationRegistry : IOrchestrationRegistry
{
	private readonly ConcurrentDictionary<string, IOrchestrationDefinition> _registry = new();
	private readonly ConcurrentDictionary<Guid, IOrchestrationDefinition> _lastestVersion = new();

	public IOrchestrationDefinition? GetDefinition(Guid idOrchestrationDefinition, int? version = null)
	{		
		if (version.HasValue)
		{
			var key = GetOrchestrationVersionKey(idOrchestrationDefinition, version.Value);
			_registry.TryGetValue(key, out var definition);
			return definition;
		}
		else
		{
			_lastestVersion.TryGetValue(idOrchestrationDefinition, out var definition);
			return definition;
		}
	}

	public void RegisterOrchestration(IOrchestrationDefinition orchestrationDefinition)
	{
		var key = GetOrchestrationVersionKey(orchestrationDefinition.IdOrchestrationDefinition, orchestrationDefinition.Version);
		_registry.AddOrUpdate(
			key,
			key =>
			{
				_lastestVersion.AddOrUpdate(
					orchestrationDefinition.IdOrchestrationDefinition,
					orchestrationDefinition,
					(idOrchestrationDefinition, def) => def.Version <= orchestrationDefinition.Version
						? orchestrationDefinition
						: def);

				return orchestrationDefinition;
			},
			(key, def) => 
				throw new InvalidOperationException($"Orchestration {orchestrationDefinition.IdOrchestrationDefinition} version {orchestrationDefinition.Version} is already registered"));
	}

	public void RegisterOrchestration<TData>(IOrchestration<TData> orchestration)
	{
		var builder = new OrchestrationBuilder<TData>();
		orchestration.Build(builder);
		var orchestrationDefinition = builder.Build(orchestration, true);

		var error = orchestrationDefinition.Validate(nameof(IOrchestrationDefinition));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		RegisterOrchestration(orchestrationDefinition);
	}

	public bool IsRegistered(Guid idOrchestrationDefinition, int version)
		=> IsRegistered(GetOrchestrationVersionKey(idOrchestrationDefinition, version));

	private static string GetOrchestrationVersionKey(Guid idOrchestrationDefinition, int version)
		=> $"{idOrchestrationDefinition}-{version}";

	private bool IsRegistered(string key)
		=> _registry.ContainsKey(key);

	public IEnumerable<IOrchestrationDefinition> GetAllDefinitions()
		=> _registry.Values;
}
