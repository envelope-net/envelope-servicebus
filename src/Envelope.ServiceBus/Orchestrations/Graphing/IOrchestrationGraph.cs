namespace Envelope.ServiceBus.Orchestrations.Graphing;

public interface IOrchestrationGraph
{
	IEnumerable<Vertex> Vertices { get; }
	IEnumerable<Edge> Edges { get; }
	IEnumerable<IOrchestrationGraph> Branches { get; }
}
