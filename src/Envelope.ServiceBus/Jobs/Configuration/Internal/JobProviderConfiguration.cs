using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.Text;
using Envelope.Transactions;
using Envelope.Validation;

namespace Envelope.ServiceBus.Jobs.Configuration.Internal;

internal class JobProviderConfiguration : IJobProviderConfiguration, IValidable
{
	public IHostInfo HostInfo { get; set; }

	internal IJobRegister JobRegister { get; set; }
	IJobRegister IJobProviderConfiguration.JobRegister
	{
		get { return JobRegister; }
		set { JobRegister = value; }
	}

	public ITransactionManagerFactory TransactionManagerFactory { get; set; }

	public Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; set; }

	public Func<IServiceProvider, IJobRepository> JobRepository { get; set; }

	public Func<IServiceProvider, IJobLogger> JobLogger { get; set; }

	public List<IValidationMessage>? Validate(
		string? propertyPrefix = null,
		List<IValidationMessage>? parentErrorBuffer = null,
		Dictionary<string, object>? validationContext = null)
	{
		if (HostInfo == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostInfo))} == null"));
		}

		if (TransactionManagerFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionManagerFactory))} == null"));
		}

		if (TransactionContextFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionContextFactory))} == null"));
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

		return parentErrorBuffer;
	}
}
