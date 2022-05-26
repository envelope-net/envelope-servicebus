using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Transformation;

public interface ICompressionProvider
{
	IMessageBody Compress(IMessageBody messageBody);

	Task<IMessageBody> CompressAsync(IMessageBody messageBody, CancellationToken cancellationToken);

	IMessageBody Extract(IMessageBody messageBody);

	Task<IMessageBody> ExtractAsync(IMessageBody messageBody, CancellationToken cancellationToken);
}
