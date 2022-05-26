using System.Collections;

namespace Envelope.ServiceBus.Orchestrations.Execution;

public class ExecutionPointerCollection : ICollection<ExecutionPointer>, IReadOnlyExecutionPointerCollection, IReadOnlyCollection<ExecutionPointer>
{
	private readonly Dictionary<Guid, ExecutionPointer> _dictionary;
	private readonly List<ExecutionPointer> _pointersSequence;

	public int Count => _pointersSequence.Count;

	public bool IsReadOnly => false;

	public ExecutionPointerCollection()
	{
		_dictionary = new();
		_pointersSequence = new();
	}

	public ExecutionPointerCollection(int capacity)
	{
		_dictionary = new Dictionary<Guid, ExecutionPointer>(capacity);
		_pointersSequence = new List<ExecutionPointer>(capacity);
	}

	public ExecutionPointerCollection(ICollection<ExecutionPointer> pointers)
	{
		if (pointers == null)
			throw new ArgumentNullException(nameof(pointers));

		_dictionary = new();
		_pointersSequence = new();

		foreach (var pointer in pointers)
			Add(pointer);
	}

	public IEnumerator<ExecutionPointer> GetEnumerator()
		=> _pointersSequence.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public void Add(ExecutionPointer pointer)
	{
		if (pointer == null)
			throw new ArgumentNullException(nameof(pointer));

		_dictionary.Add(pointer.IdExecutionPointer, pointer);
		_pointersSequence.Add(pointer);
	}

	public void Clear()
	{
		_dictionary.Clear();
		_pointersSequence.Clear();
	}

	public bool Contains(ExecutionPointer item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		return _dictionary.ContainsValue(item);
	}

	public void CopyTo(ExecutionPointer[] array, int arrayIndex)
		=> _pointersSequence.CopyTo(array, arrayIndex);

	public bool Remove(ExecutionPointer pointer)
	{
		if (pointer == null)
			throw new ArgumentNullException(nameof(pointer));

		var removed = _dictionary.Remove(pointer.IdExecutionPointer);
		if (removed)
			_pointersSequence.Remove(pointer);

		return removed;
	}

	public ExecutionPointer? FindById(Guid idExecutionPointer)
	{
		_dictionary.TryGetValue(idExecutionPointer, out var pointer);
		return pointer;
	}
}
