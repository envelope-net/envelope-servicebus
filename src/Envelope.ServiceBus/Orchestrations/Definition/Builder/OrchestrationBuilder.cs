using Envelope.Exceptions;
using Envelope.ServiceBus.Orchestrations.Definition.Internal;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Builder;

public interface IOrchestrationBuilder<TData>
{
	IReadOnlyOrchestrationStepCollection Steps { get; }

	IOrchestrationDefinition Build(IOrchestration<TData> orchestration, bool finalize = true);

	IStepBuilder<TSyncStepBody, TData> StartWith<TSyncStepBody>(string name)
		where TSyncStepBody : ISyncStepBody;

	IStepBuilder<TSyncStepBody, TData> StartWith<TSyncStepBody>(Action<StepOptionsBuilder<TSyncStepBody, TData>>? options = null)
		where TSyncStepBody : ISyncStepBody;

	IStepBuilder<ISyncInlineStepBody, TData> StartWithInline(string name, Func<IStepExecutionContext, IExecutionResult> stepAction);

	IStepBuilder<ISyncInlineStepBody, TData> StartWithInline(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<ISyncInlineStepBody, TData>>? options = null);

	IStepBuilder<TAsyncStepBody, TData> StartWithAsyncStep<TAsyncStepBody>(string name)
		where TAsyncStepBody : IAsyncStepBody;

	IStepBuilder<TAsyncStepBody, TData> StartWithAsyncStep<TAsyncStepBody>(Action<StepOptionsBuilder<TAsyncStepBody, TData>>? options = null)
		where TAsyncStepBody : IAsyncStepBody;

	IStepBuilder<IAsyncInlineStepBody, TData> StartWithAsyncInlineStep(string name, Func<IStepExecutionContext, IExecutionResult> stepAction);

	IStepBuilder<IAsyncInlineStepBody, TData> StartWithAsyncInlineStep(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<IAsyncInlineStepBody, TData>>? options = null);
}

public class OrchestrationBuilder<TData> : IOrchestrationBuilder<TData>
{
	private bool _finalized = false;

	public OrchestrationStepCollection Steps { get; }
	IReadOnlyOrchestrationStepCollection IOrchestrationBuilder<TData>.Steps => Steps;

	public OrchestrationBuilder()
	{
		Steps = new();
	}

	public IOrchestrationDefinition Build(IOrchestration<TData> orchestration, bool finalize = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (orchestration == null)
			throw new ArgumentNullException(nameof(orchestration));

		_finalized = finalize;

		if (Steps.Count == 0)
			throw new InvalidOperationException("No step defined");

		var orchestrationDefinition = 
			new OrchestrationDefinition(
				orchestration.IdOrchestrationDefinition,
				orchestration.Version,
				typeof(TData),
				Steps,
				orchestration.DefaultErrorHandling,
				orchestration.DefaultDistributedLockExpiration,
				orchestration.WorkerIdleTimeout)
			{
				Description = orchestration.Description,
				IsSingleton = orchestration.IsSingleton,
				AwaitForHandleLifeCycleEvents = orchestration.AwaitForHandleLifeCycleEvents
			};

		var error = orchestrationDefinition.Validate(nameof(IOrchestrationDefinition));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return orchestrationDefinition;
	}

	public IStepBuilder<TSyncStepBody, TData> StartWith<TSyncStepBody>(string name)
		where TSyncStepBody : ISyncStepBody
		=> StartWith<TSyncStepBody>(opt => opt.Name(name, true));

	public IStepBuilder<TSyncStepBody, TData> StartWith<TSyncStepBody>(Action<StepOptionsBuilder<TSyncStepBody, TData>>? options = null)
		where TSyncStepBody : ISyncStepBody
	{
		var name = typeof(TSyncStepBody).FullName!;
		var newOrchestrationStep = new SyncOrchestrationStep<TSyncStepBody>(name);

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TSyncStepBody, TData>(newOrchestrationStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		Steps.Add(newOrchestrationStep);

		var stepBuilder = new StepBuilder<TSyncStepBody, TData>(newOrchestrationStep, this);
		return stepBuilder;
	}

	public IStepBuilder<ISyncInlineStepBody, TData> StartWithInline(string name, Func<IStepExecutionContext, IExecutionResult> stepAction)
		=> StartWithInline(stepAction, opt => opt.Name(name, true));

	public IStepBuilder<ISyncInlineStepBody, TData> StartWithInline(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<ISyncInlineStepBody, TData>>? options = null)
	{
		if (stepAction == null)
			throw new ArgumentNullException(nameof(stepAction));

		var name = typeof(ISyncInlineStepBody).FullName!;
		var newOrchestrationStep = new InlineOrchestrationStep(stepAction)
		{
			Name = name,
			Body = stepAction
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<ISyncInlineStepBody, TData>(newOrchestrationStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		Steps.Add(newOrchestrationStep);

		var stepBuilder = new StepBuilder<ISyncInlineStepBody, TData>(newOrchestrationStep, this);
		return stepBuilder;
	}

	public IStepBuilder<TAsyncStepBody, TData> StartWithAsyncStep<TAsyncStepBody>(string name)
		where TAsyncStepBody : IAsyncStepBody
		=> StartWithAsyncStep<TAsyncStepBody>(opt => opt.Name(name, true));

	public IStepBuilder<TAsyncStepBody, TData> StartWithAsyncStep<TAsyncStepBody>(Action<StepOptionsBuilder<TAsyncStepBody, TData>>? options = null)
		where TAsyncStepBody : IAsyncStepBody
	{
		var name = typeof(TAsyncStepBody).FullName!;
		var newOrchestrationStep = new AsyncOrchestrationStep<TAsyncStepBody>(name);

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TAsyncStepBody, TData>(newOrchestrationStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		Steps.Add(newOrchestrationStep);

		var stepBuilder = new StepBuilder<TAsyncStepBody, TData>(newOrchestrationStep, this);
		return stepBuilder;
	}

	public IStepBuilder<IAsyncInlineStepBody, TData> StartWithAsyncInlineStep(string name, Func<IStepExecutionContext, IExecutionResult> stepAction)
		=> StartWithAsyncInlineStep(stepAction, opt => opt.Name(name, true));

	public IStepBuilder<IAsyncInlineStepBody, TData> StartWithAsyncInlineStep(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<IAsyncInlineStepBody, TData>>? options = null)
	{
		if (stepAction == null)
			throw new ArgumentNullException(nameof(stepAction));

		var name = typeof(IAsyncInlineStepBody).FullName!;
		var newOrchestrationStep = new InlineOrchestrationStep(stepAction)
		{
			Name = name,
			Body = stepAction
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<IAsyncInlineStepBody, TData>(newOrchestrationStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		Steps.Add(newOrchestrationStep);

		var stepBuilder = new StepBuilder<IAsyncInlineStepBody, TData>(newOrchestrationStep, this);
		return stepBuilder;
	}
}
