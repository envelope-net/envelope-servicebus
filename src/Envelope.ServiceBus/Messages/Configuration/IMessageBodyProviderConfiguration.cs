using Envelope.ServiceBus.Serialization;
using Envelope.ServiceBus.Transformation;

namespace Envelope.ServiceBus.Messages.Configuration;

public interface IMessageBodyProviderConfiguration
{
	IMessageSerializer MessageSerializer { get; set; }

	IMessageDeserializer MessageDeserializer { get; set; }

	ICompressionProvider? CompressionProvider { get; set; }

	IEncryptionProvider? EncryptionProvider { get; set; }
}
