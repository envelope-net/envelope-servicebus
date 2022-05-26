namespace Envelope.ServiceBus.Messages;

public enum MessageMetaType
{
	RequestMessage_WithResponse = 1,
	RequestMessage_Void = 2,
	Event = 3,
	Command_WithResponse = 4,
	Command_Void = 5,
	Query_WithResponse = 6,
	Response_ForRequestMessage = 7,
	Response_ForCommand = 8,
	Response_ForQuery = 9,
}
