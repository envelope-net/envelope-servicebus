namespace Envelope.ServiceBus.Orchestrations.Definition.Steps.Body;

public enum BodyType
{
	Root,
	Inline,
	If,
	IfElse,
	Switch,
	While,
	Parallel,
	WaitForEvent,
	Delay,
	Custom
}
