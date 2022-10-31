using System.Xml.Linq;

namespace Envelope.ServiceBus.Jobs.Configuration.Internal;

internal class RegisteredJob
{
	public IJob Job { get; set; }

	public Func<IServiceProvider, IJob> JobFactory { get; set; }

	private IJob ToJob(IServiceProvider serviceProvider)
		=> Job ?? JobFactory?.Invoke(serviceProvider) ?? throw new InvalidOperationException("Job == null");

	public void RegisterJob(IServiceProvider serviceProvider, IJobRegister register)
		=> register.RegisterJob(ToJob(serviceProvider));
}

internal class RegisteredJob<TData> : RegisteredJob
{
	public new IJob<TData> Job { get; set; }

	public new Func<IServiceProvider, IJob<TData>> JobFactory { get; set; }
}