namespace Envelope.ServiceBus.Messages.Internal;

internal class SendResponse<TResponse> : ISendResponse<TResponse>
{
	public Guid RequestId { get; }

	public Guid ResponseId { get; }

	public TResponse? Response { get; }

	public SendResponse(TResponse? response)
		: this(Guid.Empty, Guid.Empty, response)
	{
	}

	public SendResponse(Guid requestId, Guid responseId, TResponse? response)
	{
		RequestId = requestId;
		ResponseId = responseId;
		Response = response;
	}
}
