using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.ErrorHandling.Internal;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Graphing;
using Envelope.ServiceBus.Orchestrations.Graphing.Internal;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Orchestrations.Definition.Internal;

internal class OrchestrationDefinition : IOrchestrationDefinition, IValidable
{
	private IOrchestrationInstance? _singletonInstance;

	public Guid IdOrchestrationDefinition { get; }

	public int Version { get; }

	public string? Description { get; set; }

	public bool IsSingleton { get; set; }

	public bool AwaitForHandleLifeCycleEvents { get; set; }

	public OrchestrationStepCollection Steps { get; }
	IReadOnlyOrchestrationStepCollection IOrchestrationDefinition.Steps => Steps;

	public Type DataType { get; }

	public IErrorHandlingController DefaultErrorHandling { get; set; }

	public TimeSpan DefaultDistributedLockExpiration { get; set; }

	public TimeSpan WorkerIdleTimeout { get; set; }

	public OrchestrationDefinition(
		Guid idOrchestrationDefinition,
		int version,
		Type dataType,
		OrchestrationStepCollection steps,
		IErrorHandlingController? defaultErrorHandling,
		TimeSpan defaultDistributedLockExpiration,
		TimeSpan workerIdleTimeout)
	{
		_singletonInstance = null;
		IdOrchestrationDefinition = idOrchestrationDefinition;
		Version = version;
		Steps = steps ?? throw new ArgumentNullException(nameof(steps));
		DefaultErrorHandling = defaultErrorHandling ?? new ErrorHandlingController();
		DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
		DefaultDistributedLockExpiration = defaultDistributedLockExpiration;
		WorkerIdleTimeout = workerIdleTimeout;

		int i = 0;
		foreach (var step in steps)
		{
			if (i == 0)
				step.IsRootStep = true;

			step.OrchestrationDefinition = this;
			i++;
		}
	}

	internal void AddStep(IOrchestrationStep step)
		=> Steps.Add(step);

	void IOrchestrationDefinition.AddStep(IOrchestrationStep step)
		=> AddStep(step);

	private readonly object _singletonInstanceLock = new();
	IOrchestrationInstance? IOrchestrationDefinition.GetOrSetSingletonInstance(Func<IOrchestrationInstance> orchestrationInstanceFactory, string orchestrationKey)
	{
		lock (_singletonInstanceLock)
		{
			if (!IsSingleton)
				return null;

			if (string.IsNullOrWhiteSpace(orchestrationKey))
				throw new ArgumentNullException(nameof(orchestrationKey));

			if (_singletonInstance == null)
			{
				if (orchestrationInstanceFactory == null)
					throw new ArgumentNullException(nameof(orchestrationInstanceFactory));

				var orchestrationInstance = orchestrationInstanceFactory();
				if (orchestrationInstance == null)
					throw new InvalidOperationException($"Invalid {nameof(orchestrationInstanceFactory)} | {nameof(orchestrationInstance)} == null");

				_singletonInstance = orchestrationInstance;
			}

			if (_singletonInstance != null && _singletonInstance.OrchestrationKey != orchestrationKey)
				throw new InvalidOperationException($"Invalid {nameof(orchestrationKey)} = {orchestrationKey} | Orchestration {nameof(IdOrchestrationDefinition)} = {IdOrchestrationDefinition} has already been stardet with key = {_singletonInstance.OrchestrationKey}");

			return _singletonInstance;
		}
	}

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (DefaultDistributedLockExpiration <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"Invalid {StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DefaultDistributedLockExpiration))} <= {TimeSpan.Zero}"));
		}

		if (WorkerIdleTimeout <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"Invalid {StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(WorkerIdleTimeout))} <= {TimeSpan.Zero}"));
		}

		return parentErrorBuffer;
	}

	public IOrchestrationGraph ToGraph()
	{
		var graph = new OrchestrationGraph();

		var rootStep = Steps.FirstOrDefault();
		if (rootStep != null)
		{
			graph.AddVertext(rootStep);
			graph.AddEdges(rootStep);
		}

		return graph;
	}
}
