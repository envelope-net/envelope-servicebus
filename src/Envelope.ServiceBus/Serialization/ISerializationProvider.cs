namespace Envelope.ServiceBus.Serialization;

public interface ISerializationProvider
{
	string DefaultContentType { get; }

	IMessageSerializer GetMessageSerializer(string? contentType = null);

	bool TryGetMessageSerializer(string contentType, out IMessageSerializer serializer);

	IMessageDeserializer GetMessageDeserializer(string? contentType = null);

	bool TryGetMessageDeserializer(string contentType, out IMessageDeserializer deserializer);
}
