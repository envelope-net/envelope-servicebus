using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;

namespace Envelope.ServiceBus.Orchestrations.Graphing.Internal;

[Serializable]
internal class OrchestrationGraph : IOrchestrationGraph
{
	private readonly Dictionary<Guid, Vertex> _vertices; //Dictionary<IdStep, Vertex>
	private readonly Dictionary<Guid, List<Edge>> _edges; //Dictionary<source.IdStep, List<Edge>>
	private readonly Dictionary<Guid, OrchestrationGraph> _branches; //Dictionary<branchController.IdStep, IOrchestrationGraph>

	IEnumerable<Vertex> IOrchestrationGraph.Vertices => _vertices.Values;

	IEnumerable<Edge> IOrchestrationGraph.Edges => _edges.Values.SelectMany(x => x);
	IEnumerable<IOrchestrationGraph> IOrchestrationGraph.Branches => _branches.Values;

	public OrchestrationGraph()
	{
		_vertices = new();
		_edges = new();
		_branches = new();
	}

	public void AddVertext(IOrchestrationStep step)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));

		VertexType type = VertexType.Next;
		if (step.IsRootStep)
			type = VertexType.Root;

		if (0 < step.Branches.Count
			|| step.BodyType == typeof(WaitForEventStepBody)
			|| step.BodyType == typeof(DelayStepBody))
			type = VertexType.BranchController;

		if (step is EndOrchestrationStep)
			type = VertexType.End;

		if (step.IsStartingStep)
			type = VertexType.Branch;

		var added = _vertices.TryAdd(step.IdStep, new Vertex(step, type));
		if (!added)
			return;

		foreach (var branchStep in step.Branches.Values)
		{
			if (branchStep.StartingStep == null)
				throw new InvalidOperationException($"{nameof(branchStep.StartingStep)} == null");

			if (!_branches.TryGetValue(branchStep.StartingStep.IdStep, out var branch))
			{
				branch = new OrchestrationGraph();
				_branches.Add(branchStep.StartingStep.IdStep, branch);
			}

			branch.AddVertext(branchStep);
		}

		if (step.NextStep != null)
			AddVertext(step.NextStep);
	}

	public void AddEdges(IOrchestrationStep step)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));

		if (!_vertices.TryGetValue(step.IdStep, out var sourceVertext))
			throw new InvalidOperationException($"No source step found. | {nameof(step.IdStep)} = {step.IdStep}");

		if (_edges.ContainsKey(step.IdStep))
			return;

		var edges = new List<Edge>();
		_edges.Add(step.IdStep, edges);

		foreach (var kvp in step.Branches)
		{
			var branchStep = kvp.Value;

			if (branchStep.StartingStep == null)
				throw new InvalidOperationException($"{nameof(branchStep.StartingStep)} == null");

			if (!_branches.TryGetValue(branchStep.StartingStep.IdStep, out var branch))
				throw new InvalidOperationException($"No branch was created for step. | {nameof(step.IdStep)} = {step.IdStep}");

			if (!_vertices.TryGetValue(branchStep.IdStep, out var tagetVertext))
				if (!branch._vertices.TryGetValue(branchStep.IdStep, out tagetVertext))
					throw new InvalidOperationException($"No target nested step found. | {nameof(branchStep.IdStep)} = {branchStep.IdStep}");

			edges.Add(new Edge(sourceVertext, tagetVertext, kvp.Key.ToString() ?? "?"));

			branch.AddEdges(branchStep);
		}

		if (step.NextStep != null)
		{
			if (!_vertices.TryGetValue(step.NextStep.IdStep, out var tagetVertext))
				throw new InvalidOperationException($"No target next step found. | {nameof(step.NextStep.IdStep)} = {step.NextStep.IdStep}");

			edges.Add(new Edge(sourceVertext, tagetVertext, "Next"));
			AddEdges(step.NextStep);
		}
	}
}
