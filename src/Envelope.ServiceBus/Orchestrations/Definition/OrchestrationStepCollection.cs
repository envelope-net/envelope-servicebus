using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using System.Collections;

namespace Envelope.ServiceBus.Orchestrations.Definition;

public class OrchestrationStepCollection : ICollection<IOrchestrationStep>, IReadOnlyOrchestrationStepCollection, IReadOnlyCollection<IOrchestrationStep>
{
	private readonly Dictionary<Guid, IOrchestrationStep> _dictionary; //Dictionary<StepID, IOrchestrationStep>
	private readonly List<IOrchestrationStep> _stepsSequence;

	public int Count => _stepsSequence.Count;

	public bool IsReadOnly => false;

	public OrchestrationStepCollection()
	{
		_dictionary = new();
		_stepsSequence = new();
	}

	public OrchestrationStepCollection(int capacity)
	{
		_dictionary = new Dictionary<Guid, IOrchestrationStep>(capacity);
		_stepsSequence = new List<IOrchestrationStep>(capacity);
	}

	public OrchestrationStepCollection(ICollection<IOrchestrationStep> steps)
	{
		if (steps == null)
			throw new ArgumentNullException(nameof(steps));

		_dictionary = new();
		_stepsSequence = new();
		foreach (var step in steps)
			Add(step);
	}

	public IEnumerator<IOrchestrationStep> GetEnumerator()
		=> _stepsSequence.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public void Add(IOrchestrationStep step)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));

		_dictionary.Add(step.IdStep, step);
		_stepsSequence.Add(step);
	}

	public void Clear()
	{
		_dictionary.Clear();
		_stepsSequence.Clear();
	}

	public bool Contains(IOrchestrationStep step)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));

		return _dictionary.ContainsValue(step);
	}

	public void CopyTo(IOrchestrationStep[] array, int arrayIndex)
		=> _stepsSequence.CopyTo(array, arrayIndex);

	public bool Remove(IOrchestrationStep step)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));

		var removed = _dictionary.Remove(step.IdStep);
		if (removed)
			_stepsSequence.Remove(step);

		return removed;
	}

	public IOrchestrationStep? FindById(Guid idStep)
	{
		_dictionary.TryGetValue(idStep, out var orchestrationStep);
		return orchestrationStep;
	}
}
