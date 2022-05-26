namespace Envelope.ServiceBus.Serialization;

public interface ISerializerFactory
{
	string ContentType { get; }

	IMessageSerializer CreateSerializer();

	IMessageDeserializer CreateDeserializer();
}
