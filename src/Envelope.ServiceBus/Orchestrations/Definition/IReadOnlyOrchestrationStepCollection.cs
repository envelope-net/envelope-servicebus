using Envelope.ServiceBus.Orchestrations.Definition.Steps;

namespace Envelope.ServiceBus.Orchestrations.Definition;

public interface IReadOnlyOrchestrationStepCollection : IReadOnlyCollection<IOrchestrationStep>
{
	IOrchestrationStep? FindById(Guid idStep);
}
