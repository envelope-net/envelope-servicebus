using Envelope.Policy;

namespace Envelope.ServiceBus.Transport;

public interface IRetryable
{
	IRetryTable RetryTable { get; set; }

	TimeSpan GetDelayTime(int retryCount);
}
