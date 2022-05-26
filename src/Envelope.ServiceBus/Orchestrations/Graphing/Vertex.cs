using Envelope.ServiceBus.Orchestrations.Definition.Steps;

namespace Envelope.ServiceBus.Orchestrations.Graphing;

[Serializable]
public class Vertex : IEquatable<Vertex>
{
	public VertexType VertextType { get; }
	public IOrchestrationStep Step { get; }

	internal Vertex(IOrchestrationStep step, VertexType type)
	{
		Step = step ?? throw new ArgumentNullException(nameof(step));
		VertextType = type;
	}

	public bool Equals(Vertex? other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;
		return Guid.Equals(Step.IdStep, other.Step.IdStep);
	}

	public override bool Equals(object? obj)
	{
		if (obj is null)
			return false;
		if (ReferenceEquals(this, obj))
			return true;
		if (obj.GetType() != GetType())
			return false;
		return Equals((Vertex)obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return Step.IdStep.GetHashCode();
		}
	}

	public override string ToString()
		=> Step.Name;
}

public enum VertexType
{
	Root,
	Next,
	BranchController,
	Branch,
	End
}
