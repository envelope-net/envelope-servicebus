using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Queues;
using Envelope.Validation;

namespace Envelope.ServiceBus.Exchange.Configuration;

public interface IExchangeProviderConfiguration : IValidable
{
	IServiceBusOptions ServiceBusOptions { get; }

	internal Dictionary<string, Func<IServiceProvider, IExchange>> Exchanges { get; }

	Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }
}
