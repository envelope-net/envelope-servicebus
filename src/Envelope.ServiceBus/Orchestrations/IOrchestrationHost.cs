using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Orchestrations.Execution;
using Microsoft.Extensions.Hosting;

namespace Envelope.ServiceBus.Orchestrations;

public interface IOrchestrationHost : IOrchestrationController, IHostedService
{
	IHostInfo HostInfo { get; }
}
