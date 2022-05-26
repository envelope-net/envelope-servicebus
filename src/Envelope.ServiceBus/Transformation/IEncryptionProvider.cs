using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Transformation;

public interface IEncryptionProvider
{
	IMessageBody Encrypt(IMessageBody messageBody);

	Task<IMessageBody> EncryptAsync(IMessageBody messageBody, CancellationToken cancellationToken);

	IMessageBody Decrypt(IMessageBody messageBody);

	Task<IMessageBody> DecryptAsync(IMessageBody messageBody, CancellationToken cancellationToken);
}
