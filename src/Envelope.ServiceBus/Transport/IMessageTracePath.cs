namespace Envelope.ServiceBus.Transport;

public interface IMessageTracePath
{
	IMessageTraceNode[] Path { get; set; }
}
