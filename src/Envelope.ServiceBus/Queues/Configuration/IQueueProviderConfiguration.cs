using Envelope.ServiceBus.Configuration;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IQueueProviderConfiguration : IValidable
{
	IServiceBusOptions ServiceBusOptions { get; }

	Dictionary<string, Func<IServiceProvider, IMessageQueue>> MessageQueuesInternal { get; }

	Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }
}
