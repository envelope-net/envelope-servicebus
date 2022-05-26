namespace Envelope.ServiceBus.Messages.Resolvers;

public interface IMessageType : Envelope.Serializer.IDictionaryObject
{
	string Name { get; }
	string CrlType { get; }
	MessageMetaType MessageMetaType { get; }
	IMessageType? ResponseMessageType { get; }
}
