using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Jobs;

public interface IJobRegister
{
	ConcurrentDictionary<string, IJob> JobsInternal { get; }

	void RegisterJob(IJob job);
}
