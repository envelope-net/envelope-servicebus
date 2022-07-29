using Envelope.Calendar;
using Envelope.Text;
using Envelope.Validation;

#nullable disable

namespace Envelope.ServiceBus.Jobs.Configuration.Internal;

internal class JobConfiguration : IJobConfiguration, IValidable
{
	public string Name { get; set; }

	public bool Disabled { get; set; }

	public JobExecutingMode Mode { get; set; }

	public TimeSpan? DelayedStart { get; set; }

	public TimeSpan? IdleTimeout { get; set; }

	public CronTimerSettings CronTimerSettings { get; set; }

	public List<IValidationMessage> Validate(string propertyPrefix = null, List<IValidationMessage> parentErrorBuffer = null, Dictionary<string, object> validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(Name))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(Name))} == null"));
		}

		if ((Mode == JobExecutingMode.SequentialIntervalTimer
			|| Mode == JobExecutingMode.ExactPeriodicTimer)
			&& !IdleTimeout.HasValue)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(IdleTimeout))} == null"));
		}

		if (Mode == JobExecutingMode.Cron && CronTimerSettings == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(CronTimerSettings))} == null"));
		}

		return parentErrorBuffer;
	}
}
