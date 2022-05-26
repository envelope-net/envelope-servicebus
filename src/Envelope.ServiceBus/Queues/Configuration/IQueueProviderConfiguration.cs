using Envelope.ServiceBus.Configuration;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

public interface IQueueProviderConfiguration : IValidable
{
	IServiceBusOptions ServiceBusOptions { get; }

	internal Dictionary<string, Func<IServiceProvider, IMessageQueue>> MessageQueues { get; }

	Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }
}
