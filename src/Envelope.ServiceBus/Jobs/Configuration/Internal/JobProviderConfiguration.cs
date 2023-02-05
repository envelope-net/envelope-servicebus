using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.ServiceBus.Queries;
using Envelope.ServiceBus.Writers;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration.Internal;

internal class JobProviderConfiguration : IJobProviderConfiguration, IValidable
{
	public IHostInfo HostInfoInternal { get; set; }

	internal IJobRegister JobRegister { get; set; }

	IJobRegister IJobProviderConfiguration.JobRegisterInternal
	{
		get { return JobRegister; }
		set { JobRegister = value; }
	}

	public Func<IServiceProvider, IJobRepository> JobRepository { get; set; }

	public Func<IServiceProvider, IJobLogger> JobLogger { get; set; }

	public Func<IServiceProvider, IServiceBusReader> ServiceBusReader { get; set; }

	public Func<IServiceProvider, IJobMessageWriter> JobMessageWriter { get; set; }

	public List<IValidationMessage>? Validate(
		string? propertyPrefix = null,
		List<IValidationMessage>? parentErrorBuffer = null,
		Dictionary<string, object>? validationContext = null)
	{
		if (HostInfoInternal == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostInfoInternal))} == null"));
		}

		if (JobRepository == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(JobRepository))} == null"));
		}

		if (JobLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(JobLogger))} == null"));
		}

		if (ServiceBusReader == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ServiceBusReader))} == null"));
		}

		if (JobMessageWriter == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(JobMessageWriter))} == null"));
		}

		return parentErrorBuffer;
	}
}
