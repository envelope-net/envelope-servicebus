using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.Transactions;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration;

public interface IJobProviderConfiguration : IValidable
{
	internal IHostInfo HostInfo { get; set; }

	internal IJobRegister JobRegister { get; set; }

	Func<IServiceProvider, IJobRepository> JobRepository { get; set;}

	Func<IServiceProvider, IJobLogger> JobLogger { get; set; }
}
