using Envelope.Exceptions;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Orchestrations.Definition.Steps;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Definition.Steps.Internal;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations.Definition.Builder;

public interface IStepOptionsBuilder<TBuilder, TData>
	where TBuilder : IStepOptionsBuilder<TBuilder, TData>
{
	IOrchestrationStep CurrentStep { get; }

	IOrchestrationStep Build(bool finalize = false);

	TBuilder Name(string name, bool force = true);

	TBuilder SetInput(Action<TData, IStepExecutionContext> action, bool force = true);

	TBuilder SetOutput(Action<TData, IStepExecutionContext> action, bool force = true);

	TBuilder ErrorHandlingController(IErrorHandlingController errorHandlingController, bool force = true);

	TBuilder DistributedLockExpiration(TimeSpan? distributedLockExpiration, bool force = true);
}

public abstract class StepOptionsBuilderBase<TBuilder, TData> : IStepOptionsBuilder<TBuilder, TData>
	where TBuilder : StepOptionsBuilderBase<TBuilder, TData>
{
	protected readonly TBuilder _builder;
	protected bool _finalized = false;

	public IOrchestrationStep CurrentStep { get; private set; }

	public StepOptionsBuilderBase(IOrchestrationStep orchestrationStep)
	{
		CurrentStep = orchestrationStep ?? throw new ArgumentNullException(nameof(orchestrationStep));
		_builder = (TBuilder)this;
	}

	public IOrchestrationStep Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = CurrentStep.Validate(nameof(OrchestrationStep))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return CurrentStep;
	}

	public TBuilder Name(string name, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(CurrentStep.Name))
			CurrentStep.Name = !string.IsNullOrWhiteSpace(name)
				? name
				: throw new ArgumentNullException(nameof(name));

		return _builder;
	}

	public TBuilder SetInput(Action<TData, IStepExecutionContext> action, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || CurrentStep.SetInputParameters == null)
			CurrentStep.SetInputParameters = (stepBody, data, ctx) => action((TData)data, ctx);

		return _builder;
	}

	public TBuilder SetOutput(Action<TData, IStepExecutionContext> action, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || CurrentStep.SetOutputParameters == null)
			CurrentStep.SetOutputParameters = (stepBody, data, ctx) => action((TData)data, ctx);

		return _builder;
	}

	public TBuilder ErrorHandlingController(IErrorHandlingController errorHandlingController, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || CurrentStep.ErrorHandlingController == null)
			CurrentStep.ErrorHandlingController = errorHandlingController;

		return _builder;
	}

	public TBuilder DistributedLockExpiration(TimeSpan? distributedLockExpiration, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !CurrentStep.DistributedLockExpiration.HasValue)
			CurrentStep.DistributedLockExpiration = distributedLockExpiration;

		return _builder;
	}
}

public class StepOptionsBuilder<TData> : StepOptionsBuilderBase<StepOptionsBuilder<TData>, TData>
{
	public StepOptionsBuilder(IOrchestrationStep orchestrationStep)
		: base(orchestrationStep)
	{
	}
}






public interface IStepOptionsBuilder<TBuilder, TStepBody, TData> : IStepOptionsBuilder<TBuilder, TData>
	where TBuilder : IStepOptionsBuilder<TBuilder, TStepBody, TData>
	where TStepBody : IStepBody
{
	TBuilder SetInput(Action<TStepBody, TData, IStepExecutionContext> action, bool force = true);

	TBuilder SetOutput(Action<TStepBody, TData, IStepExecutionContext> action, bool force = true);
}

public abstract class StepOptionsBuilderBase<TBuilder, TStepBody, TData> : StepOptionsBuilderBase<TBuilder, TData> , IStepOptionsBuilder<TBuilder, TStepBody, TData>, IStepOptionsBuilder<TBuilder, TData>
	where TBuilder : StepOptionsBuilderBase<TBuilder, TStepBody, TData>
	where TStepBody : IStepBody
{
	public StepOptionsBuilderBase(IOrchestrationStep orchestrationStep)
		: base(orchestrationStep)
	{
	}

	public TBuilder SetInput(Action<TStepBody, TData, IStepExecutionContext> action, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || CurrentStep.SetInputParameters == null)
			CurrentStep.SetInputParameters = (stepBody, data, ctx) => action((TStepBody)stepBody, (TData)data, ctx);

		return _builder;
	}

	public TBuilder SetOutput(Action<TStepBody, TData, IStepExecutionContext> action, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || CurrentStep.SetOutputParameters == null)
			CurrentStep.SetOutputParameters = (stepBody, data, ctx) => action((TStepBody)stepBody, (TData)data, ctx);

		return _builder;
	}
}

public class StepOptionsBuilder<TStepBody, TData> : StepOptionsBuilderBase<StepOptionsBuilder<TStepBody, TData>, TStepBody, TData>
	where TStepBody : IStepBody
{
	public StepOptionsBuilder(IOrchestrationStep orchestrationStep)
		: base(orchestrationStep)
	{
	}
}