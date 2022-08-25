using Envelope.ServiceBus.Configuration;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

public interface IQueueProviderConfiguration : IValidable
{
	IServiceBusOptions ServiceBusOptions { get; }

	Dictionary<string, Func<IServiceProvider, IMessageQueue>> MessageQueuesInternal { get; }

	Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }
}
