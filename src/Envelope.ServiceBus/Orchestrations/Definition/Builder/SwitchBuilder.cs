namespace Envelope.ServiceBus.Orchestrations.Definition.Builder;

public interface ISwitchBuilder<TData>
{
	ISwitchBuilder<TData> Case(object @object, Action<IOrchestrationBuilder<TData>> configureCaseBranche);
}

internal class SwitchBuilder<TData> : ISwitchBuilder<TData>
{
	internal Dictionary<object, Action<IOrchestrationBuilder<TData>>> Cases { get; }

	public SwitchBuilder()
	{
		Cases = new();
	}

	public ISwitchBuilder<TData> Case(object @object, Action<IOrchestrationBuilder<TData>> configureCaseBranche)
	{
		if (@object == null)
			throw new ArgumentNullException(nameof(@object));

		Cases[@object] = configureCaseBranche ?? throw new ArgumentNullException(nameof(configureCaseBranche));
		return this;
	}
}
