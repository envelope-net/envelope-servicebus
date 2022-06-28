using Envelope.Exceptions;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Builder;

public interface IStepBuilder<TStepBody, TData>
	where TStepBody : IStepBody
{
	IOrchestrationStep CurrentStep { get; }

	internal IOrchestrationStep Validate(bool finalize = false);

	IStepBuilder<TStepBody, TData> Then<TSyncStepBody>(string name)
		where TSyncStepBody : ISyncStepBody;

	IStepBuilder<TStepBody, TData> Then<TSyncStepBody>(Action<StepOptionsBuilder<TSyncStepBody, TData>>? options = null)
		where TSyncStepBody : ISyncStepBody;

	IStepBuilder<TStepBody, TData> ThenInline(string name, Func<IStepExecutionContext, IExecutionResult> stepAction);

	IStepBuilder<TStepBody, TData> ThenInline(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<ISyncInlineStepBody, TData>>? options = null);

	IStepBuilder<TStepBody, TData> ThenAsyncStep<TAsyncStepBody>(string name)
		where TAsyncStepBody : IAsyncStepBody;

	IStepBuilder<TStepBody, TData> ThenAsyncStep<TAsyncStepBody>(Action<StepOptionsBuilder<TAsyncStepBody, TData>>? options = null)
		where TAsyncStepBody : IAsyncStepBody;

	IStepBuilder<TStepBody, TData> ThenAsyncInlineStep(string name, Func<IStepExecutionContext, IExecutionResult> stepAction);

	IStepBuilder<TStepBody, TData> ThenAsyncInlineStep(
		Func<IStepExecutionContext, IExecutionResult> stepAction,
		Action<StepOptionsBuilder<IAsyncInlineStepBody, TData>>? options = null);

	IStepBuilder<TStepBody, TData> AttachOrchestration(Action<IOrchestrationBuilder<TData>> nestedOrchestration);

	IStepBuilder<TStepBody, TData> If(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch);

	IStepBuilder<TStepBody, TData> If(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> IfElse(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<IOrchestrationBuilder<TData>> configureElseBranch);

	IStepBuilder<TStepBody, TData> IfElse(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<IOrchestrationBuilder<TData>> configureElseBranch,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> Switch(
		string name,
		Func<IStepExecutionContext, object> @case,
		Action<ISwitchBuilder<TData>> configureCases);

	IStepBuilder<TStepBody, TData> Switch(
		Func<IStepExecutionContext, object> @case,
		Action<ISwitchBuilder<TData>> configureCases,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> Switch(
		string name,
		Func<IStepExecutionContext, object> @case,
		Dictionary<object, Action<IOrchestrationBuilder<TData>>> configureCaseBranches);

	IStepBuilder<TStepBody, TData> Switch(
		Func<IStepExecutionContext, object> @case,
		Dictionary<object, Action<IOrchestrationBuilder<TData>>> configureCaseBranches,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> While(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureWhileBranch);

	IStepBuilder<TStepBody, TData> While(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureWhileBranch,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> Parallel(
		string name,
		Action<IParallelBuilder<TData>> configureBranches);

	IStepBuilder<TStepBody, TData> Parallel(
		Action<IParallelBuilder<TData>> configureBranches,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> Parallel(
		string name,
		List<Action<IOrchestrationBuilder<TData>>> configureParallelBranches);

	IStepBuilder<TStepBody, TData> Parallel(
		List<Action<IOrchestrationBuilder<TData>>> configureParallelBranches,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> WaitFor(
		string name,
		string eventName,
		string? eventKey = null,
		DateTime? timeToLiveUtc = null);

	IStepBuilder<TStepBody, TData> WaitFor(
		string eventName,
		string? eventKey = null,
		DateTime? timeToLiveUtc = null,
		Action<StepOptionsBuilder<TData>>? options = null);

	IStepBuilder<TStepBody, TData> Delay(string name, TimeSpan delayInterval);

	IStepBuilder<TStepBody, TData> Delay(TimeSpan delayInterval, Action<StepOptionsBuilder<TData>>? options = null);

	void End(Action<TData, IStepExecutionContext>? inputAction = null);
}

public class StepBuilder<TStepBody, TData> : IStepBuilder<TStepBody, TData>
	where TStepBody : IStepBody
{
	private bool _finalized = false;
	protected OrchestrationBuilder<TData> _orchestrationBuilder;

	public IOrchestrationStep CurrentStep { get; private set; }

	public StepBuilder(IOrchestrationStep orchestrationStep, OrchestrationBuilder<TData> orchestrationBuilder)
	{
		_orchestrationBuilder = orchestrationBuilder ?? throw new ArgumentNullException(nameof(orchestrationBuilder));
		CurrentStep = orchestrationStep ?? throw new ArgumentNullException(nameof(orchestrationStep));
	}

	internal IOrchestrationStep Validate(bool finalize = false)
	{
		_finalized = finalize;

		var error = CurrentStep.Validate(nameof(OrchestrationStep));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return CurrentStep;
	}

	IOrchestrationStep IStepBuilder<TStepBody, TData>.Validate(bool finalize)
		=> Validate(finalize);

	public IStepBuilder<TStepBody, TData> Then<TSyncStepBody>(string name)
		where TSyncStepBody : ISyncStepBody
		=> Then<TSyncStepBody>(opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Then<TSyncStepBody>(Action<StepOptionsBuilder<TSyncStepBody, TData>>? options = null)
		where TSyncStepBody : ISyncStepBody
	{
		var name = typeof(TSyncStepBody).FullName!;
		var newOrchestrationStep = new SyncOrchestrationStep<TSyncStepBody> (name);

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TSyncStepBody, TData>(newOrchestrationStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		CurrentStep.NextStep = newOrchestrationStep;
		CurrentStep = newOrchestrationStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> ThenInline(string name, Func<IStepExecutionContext, IExecutionResult> stepAction)
		=> ThenInline(stepAction, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> ThenInline(
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

		_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		CurrentStep.NextStep = newOrchestrationStep;
		CurrentStep = newOrchestrationStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> ThenAsyncStep<TAsyncStepBody>(string name)
		where TAsyncStepBody : IAsyncStepBody
		=> ThenAsyncStep<TAsyncStepBody>(opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> ThenAsyncStep<TAsyncStepBody>(Action<StepOptionsBuilder<TAsyncStepBody, TData>>? options = null)
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

		_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		CurrentStep.NextStep = newOrchestrationStep;
		CurrentStep = newOrchestrationStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> ThenAsyncInlineStep(string name, Func<IStepExecutionContext, IExecutionResult> stepAction)
		=> ThenAsyncInlineStep(stepAction, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> ThenAsyncInlineStep(
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

		_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		CurrentStep.NextStep = newOrchestrationStep;
		CurrentStep = newOrchestrationStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> AttachOrchestration(Action<IOrchestrationBuilder<TData>> nestedOrchestration)
	{
		if (nestedOrchestration == null)
			throw new ArgumentNullException(nameof(nestedOrchestration));

		var orchestrationBuilder = new OrchestrationBuilder<TData>();
		nestedOrchestration.Invoke(orchestrationBuilder);
		var newOrchestrationSteps = orchestrationBuilder.Steps.ToList();

		if (newOrchestrationSteps.Count == 0)
			throw new ArgumentException($"{nameof(nestedOrchestration)} has no step", nameof(nestedOrchestration));

		IOrchestrationStep? lastNextStep = null;
		for (int i = 0; i < newOrchestrationSteps.Count; i++)
		{
			var newOrchestrationStep = newOrchestrationSteps[i];

			//first step
			if (i == 0)
				CurrentStep.NextStep = newOrchestrationStep;

			_orchestrationBuilder.Steps.Add(newOrchestrationStep);
			
			//if step is not in branch
			if (newOrchestrationStep.BranchController == null)
				lastNextStep = newOrchestrationStep;
		}

		if (lastNextStep == null)
			throw new InvalidOperationException($"{nameof(lastNextStep)} == null");

		CurrentStep = lastNextStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> If(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch)
		=> If(condition, configureIfBranch, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> If(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (condition == null)
			throw new ArgumentNullException(nameof(condition));

		if (configureIfBranch == null)
			throw new ArgumentNullException(nameof(configureIfBranch));

		var name = "if";
		var ifStep = new SyncOrchestrationStep<IfStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) => ((IfStepBody)stepBody).Condition = condition
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(ifStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(ifStep);
		CurrentStep.NextStep = ifStep;
		CurrentStep = ifStep;

		var orchestrationBuilder = new OrchestrationBuilder<TData>();
		configureIfBranch.Invoke(orchestrationBuilder);
		var firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

		if (firstBranchStep == null)
			throw new ArgumentException($"{nameof(configureIfBranch)} has no step", nameof(configureIfBranch));

		foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
		{
			if (newOrchestrationStep.StartingStep == null)
				newOrchestrationStep.StartingStep = firstBranchStep;

			if (newOrchestrationStep.BranchController == null)
				newOrchestrationStep.BranchController = ifStep;

			_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		}

		ifStep.Branches.Add(true, firstBranchStep);

		return this;
	}

	public IStepBuilder<TStepBody, TData> IfElse(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<IOrchestrationBuilder<TData>> configureElseBranch)
		=> IfElse(condition, configureIfBranch, configureElseBranch, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> IfElse(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureIfBranch,
		Action<IOrchestrationBuilder<TData>> configureElseBranch,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (condition == null)
			throw new ArgumentNullException(nameof(condition));

		if (configureIfBranch == null)
			throw new ArgumentNullException(nameof(configureIfBranch));

		if (configureElseBranch == null)
			throw new ArgumentNullException(nameof(configureElseBranch));

		var name = "if-else";
		var ifElseStep = new SyncOrchestrationStep<IfElseStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) => ((IfElseStepBody)stepBody).Condition = condition
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(ifElseStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(ifElseStep);
		CurrentStep.NextStep = ifElseStep;
		CurrentStep = ifElseStep;

		var orchestrationBuilder = new OrchestrationBuilder<TData>();
		configureIfBranch.Invoke(orchestrationBuilder);
		var firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

		if (firstBranchStep == null)
			throw new ArgumentException($"{nameof(configureIfBranch)} has no step", nameof(configureIfBranch));

		foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
		{
			if (newOrchestrationStep.StartingStep == null)
				newOrchestrationStep.StartingStep = firstBranchStep;

			if (newOrchestrationStep.BranchController == null)
				newOrchestrationStep.BranchController = ifElseStep;

			_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		}

		ifElseStep.Branches.Add(true, firstBranchStep);


		orchestrationBuilder = new OrchestrationBuilder<TData>();
		configureElseBranch.Invoke(orchestrationBuilder);
		firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

		if (firstBranchStep == null)
			throw new ArgumentException($"{nameof(configureElseBranch)} has no step", nameof(configureElseBranch));

		foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
		{
			if (newOrchestrationStep.StartingStep == null)
				newOrchestrationStep.StartingStep = firstBranchStep;

			if (newOrchestrationStep.BranchController == null)
				newOrchestrationStep.BranchController = ifElseStep;

			_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		}

		ifElseStep.Branches.Add(false, firstBranchStep);

		return this;
	}

	public IStepBuilder<TStepBody, TData> Switch(
		string name,
		Func<IStepExecutionContext, object> @case,
		Action<ISwitchBuilder<TData>> configureCases)
		=> Switch(@case, configureCases, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Switch(
		Func<IStepExecutionContext, object> @case,
		Action<ISwitchBuilder<TData>> configureCases,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (configureCases == null)
			throw new ArgumentNullException(nameof(configureCases));

		var switchBuilder = new SwitchBuilder<TData>();
		configureCases.Invoke(switchBuilder);

		return Switch(@case, switchBuilder.Cases, options);
	}

	public IStepBuilder<TStepBody, TData> Switch(
		string name,
		Func<IStepExecutionContext, object> @case,
		Dictionary<object, Action<IOrchestrationBuilder<TData>>> configureCaseBranches)
		=> Switch(@case, configureCaseBranches, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Switch(
		Func<IStepExecutionContext, object> @case,
		Dictionary<object, Action<IOrchestrationBuilder<TData>>> configureCaseBranches,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (@case == null)
			throw new ArgumentNullException(nameof(@case));

		if (configureCaseBranches == null || configureCaseBranches.Count == 0)
			throw new ArgumentNullException(nameof(configureCaseBranches));

		var name = "switch";
		var switchStep = new SyncOrchestrationStep<SwitchStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) => ((SwitchStepBody)stepBody).Case = @case
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(switchStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(switchStep);
		CurrentStep.NextStep = switchStep;
		CurrentStep = switchStep;

		var i = 0;
		foreach (var kvp in configureCaseBranches)
		{
			var configureCaseBranche = kvp.Value;

			if (configureCaseBranche == null)
				throw new ArgumentException($"{nameof(configureCaseBranches)}[{i}] == null", nameof(configureCaseBranches));

			var orchestrationBuilder = new OrchestrationBuilder<TData>();
			configureCaseBranche.Invoke(orchestrationBuilder);
			var firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

			if (firstBranchStep == null)
				throw new ArgumentException($"{nameof(configureCaseBranches)}[{i}] has no step", nameof(configureCaseBranches));

			foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
			{
				if (newOrchestrationStep.StartingStep == null)
					newOrchestrationStep.StartingStep = firstBranchStep;

				if (newOrchestrationStep.BranchController == null)
					newOrchestrationStep.BranchController = switchStep;

				_orchestrationBuilder.Steps.Add(newOrchestrationStep);
			}

			switchStep.Branches.Add(kvp.Key, firstBranchStep);

			i++;
		}

		return this;
	}

	public IStepBuilder<TStepBody, TData> While(
		string name,
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureWhileBranch)
		=> While(condition, configureWhileBranch, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> While(
		Func<IStepExecutionContext, bool> condition,
		Action<IOrchestrationBuilder<TData>> configureWhileBranch,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (condition == null)
			throw new ArgumentNullException(nameof(condition));

		if (configureWhileBranch == null)
			throw new ArgumentNullException(nameof(configureWhileBranch));

		var name = "while";
		var whileStep = new SyncOrchestrationStep<WhileStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) => ((WhileStepBody)stepBody).Condition = condition
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(whileStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(whileStep);
		CurrentStep.NextStep = whileStep;
		CurrentStep = whileStep;

		var orchestrationBuilder = new OrchestrationBuilder<TData>();
		configureWhileBranch.Invoke(orchestrationBuilder);
		var firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

		if (firstBranchStep == null)
			throw new ArgumentException($"{nameof(configureWhileBranch)} has no step", nameof(configureWhileBranch));

		foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
		{
			if (newOrchestrationStep.StartingStep == null)
				newOrchestrationStep.StartingStep = firstBranchStep;

			if (newOrchestrationStep.BranchController == null)
				newOrchestrationStep.BranchController = whileStep;

			_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		}

		whileStep.Branches.Add(true, firstBranchStep);

		return this;
	}

	public IStepBuilder<TStepBody, TData> Parallel(
		string name,
		Action<IParallelBuilder<TData>> configureBranches)
		=> Parallel(configureBranches, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Parallel(
		Action<IParallelBuilder<TData>> configureBranches,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (configureBranches == null)
			throw new ArgumentNullException(nameof(configureBranches));

		var parallelBuilder = new ParallelBuilder<TData>();
		configureBranches.Invoke(parallelBuilder);

		return Parallel(parallelBuilder.Branches, options);
	}

	public IStepBuilder<TStepBody, TData> Parallel(
		string name,
		List<Action<IOrchestrationBuilder<TData>>> configureParallelBranches)
		=> Parallel(configureParallelBranches, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Parallel(
		List<Action<IOrchestrationBuilder<TData>>> configureParallelBranches,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (configureParallelBranches == null || configureParallelBranches.Count == 0)
			throw new ArgumentNullException(nameof(configureParallelBranches));

		var name = "parallel";
		var parallelStep = new SyncOrchestrationStep<ParallelStepBody>(name);

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(parallelStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(parallelStep);
		CurrentStep.NextStep = parallelStep;
		CurrentStep = parallelStep;

		var i = 0;
		foreach (var configureParallelBranche in configureParallelBranches)
		{
			if (configureParallelBranche == null)
				throw new ArgumentException($"{nameof(configureParallelBranches)}[{i}] == null", nameof(configureParallelBranches));

			var orchestrationBuilder = new OrchestrationBuilder<TData>();
			configureParallelBranche.Invoke(orchestrationBuilder);
			var firstBranchStep = orchestrationBuilder.Steps.FirstOrDefault();

			if (firstBranchStep == null)
				throw new ArgumentException($"{nameof(configureParallelBranches)}[{i}] has no step", nameof(configureParallelBranches));

			foreach (var newOrchestrationStep in orchestrationBuilder.Steps)
			{
				if (newOrchestrationStep.StartingStep == null)
					newOrchestrationStep.StartingStep = firstBranchStep;

				if (newOrchestrationStep.BranchController == null)
					newOrchestrationStep.BranchController = parallelStep;

				_orchestrationBuilder.Steps.Add(newOrchestrationStep);
			}

			parallelStep.Branches.Add(i, firstBranchStep);

			i++;
		}

		return this;
	}

	public IStepBuilder<TStepBody, TData> WaitFor(
		string name,
		string eventName,
		string? eventKey = null,
		DateTime? timeToLiveUtc = null)
		=> WaitFor(eventName, eventKey, timeToLiveUtc, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> WaitFor(
		string eventName,
		string? eventKey = null,
		DateTime? timeToLiveUtc = null,
		Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (string.IsNullOrWhiteSpace(eventName))
			throw new ArgumentNullException(nameof(eventName));

		var name = $"wait for '{eventName}'";
		var waitForEventStep = new SyncOrchestrationStep<WaitForEventStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) =>
			{
				var waitForEventStepBody = (WaitForEventStepBody)stepBody;
				waitForEventStepBody.EventName = eventName;
				waitForEventStepBody.EventKey = eventKey;
				waitForEventStepBody.TimeToLiveUtc = timeToLiveUtc;
			}
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(waitForEventStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(waitForEventStep);
		CurrentStep.NextStep = waitForEventStep;
		CurrentStep = waitForEventStep;

		return this;
	}

	public IStepBuilder<TStepBody, TData> Delay(string name, TimeSpan delayInterval)
		=> Delay(delayInterval, opt => opt.Name(name, true));

	public IStepBuilder<TStepBody, TData> Delay(TimeSpan delayInterval, Action<StepOptionsBuilder<TData>>? options = null)
	{
		if (delayInterval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(delayInterval));

		var name = $"delay {delayInterval}";

		var delayStep = new SyncOrchestrationStep<DelayStepBody>(name)
		{
			SetInputParameters = (stepBody, data, context) => ((DelayStepBody)stepBody).DelayInterval = delayInterval
		};

		if (options != null)
		{
			var optionsBuilder = new StepOptionsBuilder<TData>(delayStep);
			options.Invoke(optionsBuilder);
			optionsBuilder.Name(name, false);
		}

		_orchestrationBuilder.Steps.Add(delayStep);
		CurrentStep.NextStep = delayStep;
		CurrentStep = delayStep;

		return this;
	}

	public void End(Action<TData, IStepExecutionContext>? inputAction = null)
	{
		var name = "end";
		var newOrchestrationStep = new EndOrchestrationStep(name);

		if (inputAction != null)
		{
			newOrchestrationStep.SetInputParameters = (stepBody, data, context) =>
			{
				inputAction.Invoke((TData)data, context);
			};
		}

		_orchestrationBuilder.Steps.Add(newOrchestrationStep);
		CurrentStep.NextStep = newOrchestrationStep;
		CurrentStep = null!;
	}
}