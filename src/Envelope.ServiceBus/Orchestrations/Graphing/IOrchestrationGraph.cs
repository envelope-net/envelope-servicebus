namespace Envelope.ServiceBus.Orchestrations.Graphing;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IOrchestrationGraph
{
	IEnumerable<Vertex> Vertices { get; }
	IEnumerable<Edge> Edges { get; }
	IEnumerable<IOrchestrationGraph> Branches { get; }
}
