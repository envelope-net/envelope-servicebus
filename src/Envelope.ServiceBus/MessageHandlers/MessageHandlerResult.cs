using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.MessageHandlers;

public class MessageHandlerResult
{
	public DateTime CreatedUtc { get; }

	public bool Processed { get; internal set; }

	public MessageStatus MessageStatus { get; internal set; }

	public IResult? ErrorResult { get; internal set; }

	public bool Retry { get; internal set; }

	public TimeSpan? RetryInterval { get; internal set; }

	internal MessageHandlerResult()
	{
		CreatedUtc = DateTime.UtcNow;
	}

	public DateTime GetDelayedToUtc(TimeSpan retryInterval)
		=> CreatedUtc.Add(retryInterval);
}

public class MessageHandlerResult<TResponse> : MessageHandlerResult
{
	public TResponse? Result { get; internal set; }

	internal MessageHandlerResult()
		: base()
	{
	}
}
