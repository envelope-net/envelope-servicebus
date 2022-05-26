namespace Envelope.ServiceBus.Orchestrations.Graphing;

[Serializable]
public class Edge : IEquatable<Edge>
{
	public Vertex From { get; }

	public Vertex To { get; }

	public string Title { get; }


	public Edge(Vertex from, Vertex to, string title)
	{
		From = from;
		To = to;
		Title = title ?? "?";
	}

	public bool Equals(Edge? other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;
		return Equals(To, other.To) && Equals(From, other.From) && string.Equals(Title, other.Title);
	}

	public override bool Equals(object? obj)
	{
		if (obj is null)
			return false;
		if (ReferenceEquals(this, obj))
			return true;
		if (obj.GetType() != GetType())
			return false;
		return Equals((Edge)obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = To?.GetHashCode() ?? 0;
			hashCode = (hashCode * 397) ^ (From?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (Title?.GetHashCode() ?? 0);
			return hashCode;
		}
	}

	public override string ToString()
		=> $"{From.Step.Name} -> {To.Step.Name}";
}
