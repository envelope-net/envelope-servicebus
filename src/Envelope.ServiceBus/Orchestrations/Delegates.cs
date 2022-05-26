using Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;
using Envelope.ServiceBus.Orchestrations.Execution;

namespace Envelope.ServiceBus.Orchestrations;

public delegate void Assign<TStepBody, TData>(TStepBody body, TData data, IStepExecutionContext context)
	where TStepBody : IStepBody;

public delegate void AssignParameters(object body, object data, IStepExecutionContext context);