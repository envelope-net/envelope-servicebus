namespace Envelope.ServiceBus.Jobs.Configuration.Internal;

internal class RegisteredJob
{
	public IJob Job { get; set; }

	public Func<IServiceProvider, IJob> JobFactory { get; set; }

	private IJob ToJob(IServiceProvider serviceProvider)
		=> Job ?? JobFactory?.Invoke(serviceProvider) ?? throw new InvalidOperationException("Job == null");

	public void JobRegister(IServiceProvider serviceProvider, IJobRegister register)
		=> register.RegisterJob(ToJob(serviceProvider));
}