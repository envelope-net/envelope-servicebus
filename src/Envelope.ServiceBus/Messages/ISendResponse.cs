namespace Envelope.ServiceBus.Messages;

public interface ISendResponse<TResponse>
{
	Guid RequestId { get; }
	Guid ResponseId { get; }
	TResponse? Response { get; }
}
