namespace Envelope.ServiceBus.Orchestrations.Definition.Builder;

public interface IParallelBuilder<TData>
{
	IParallelBuilder<TData> Branch(Action<IOrchestrationBuilder<TData>> configureCaseBranche);
}

internal class ParallelBuilder<TData> : IParallelBuilder<TData>
{
	internal List<Action<IOrchestrationBuilder<TData>>> Branches { get; }

	public ParallelBuilder()
	{
		Branches = new();
	}

	public IParallelBuilder<TData> Branch(Action<IOrchestrationBuilder<TData>> configureCaseBranche)
	{
		if (configureCaseBranche == null)
			throw new ArgumentNullException(nameof(configureCaseBranche));

		Branches.Add(configureCaseBranche);
		return this;
	}
}
