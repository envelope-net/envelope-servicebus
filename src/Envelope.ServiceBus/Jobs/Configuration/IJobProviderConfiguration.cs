using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.ServiceBus.Queries;
using Envelope.ServiceBus.Writers;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IJobProviderConfiguration : IValidable
{
	IHostInfo HostInfoInternal { get; set; }

	IJobRegister JobRegisterInternal { get; set; }

	Func<IServiceProvider, IJobRepository> JobRepository { get; set;}

	Func<IServiceProvider, IJobLogger> JobLogger { get; set; }

	Func<IServiceProvider, IServiceBusReader> ServiceBusReader { get; set; }

	Func<IServiceProvider, IJobMessageWriter> JobMessageWriter { get; set; }
}
