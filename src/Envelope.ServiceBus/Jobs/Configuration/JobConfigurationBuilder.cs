using Envelope.Calendar;
using Envelope.Exceptions;
using Envelope.ServiceBus.Jobs.Configuration.Internal;

namespace Envelope.ServiceBus.Jobs.Configuration;

public interface IJobConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IJobConfigurationBuilder<TBuilder, TObject>
	where TObject : IJobConfiguration
{
	TBuilder Object(TObject jobConfiguration);

	TObject Build(bool finalize = false);

	TBuilder Name(string name, bool force = true);

	TBuilder Disabled(bool disabled);

	TBuilder Mode(JobExecutingMode jobExecutingMode);

	TBuilder DelayedStart(TimeSpan? delayedStart, bool force = true);

	TBuilder IdleTimeout(TimeSpan? idleTimeout, bool force = true);

	TBuilder CronTimerSettings(CronTimerSettings cronTimerSettings, bool force = true);
}

public abstract class JobConfigurationBuilderBase<TBuilder, TObject> : IJobConfigurationBuilder<TBuilder, TObject>
	where TBuilder : JobConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IJobConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _jobConfiguration;

	protected JobConfigurationBuilderBase(TObject jobConfiguration)
	{
		_jobConfiguration = jobConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject jobConfiguration)
	{
		_jobConfiguration = jobConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _jobConfiguration.Validate(nameof(IJobConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _jobConfiguration;
	}

	public TBuilder Name(string name, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_jobConfiguration.Name))
			_jobConfiguration.Name = name;

		return _builder;
	}

	public TBuilder Disabled(bool disabled)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_jobConfiguration.Disabled = disabled;
		return _builder;
	}

	public TBuilder Mode(JobExecutingMode jobExecutingMode)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_jobConfiguration.Mode = jobExecutingMode;
		return _builder;
	}

	public TBuilder DelayedStart(TimeSpan? delayedStart, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_jobConfiguration.DelayedStart.HasValue)
			_jobConfiguration.DelayedStart = delayedStart;

		return _builder;
	}

	public TBuilder IdleTimeout(TimeSpan? idleTimeout, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_jobConfiguration.IdleTimeout.HasValue)
			_jobConfiguration.IdleTimeout = idleTimeout;

		return _builder;
	}

	public TBuilder CronTimerSettings(CronTimerSettings cronTimerSettings, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobConfiguration.CronTimerSettings == null)
			_jobConfiguration.CronTimerSettings = cronTimerSettings;

		return _builder;
	}
}

public class JobConfigurationBuilder : JobConfigurationBuilderBase<JobConfigurationBuilder, IJobConfiguration>
{
	public JobConfigurationBuilder()
		: base(new JobConfiguration())
	{
	}

	public static JobConfigurationBuilder GetDefaultBuilder()
		=> new JobConfigurationBuilder()
			//.Name(null)
			//.Disabled(false)
			.Mode(JobExecutingMode.SequentialIntervalTimer)
			//.DelayedStart(null)
			//.IdleTimeout(null)
			//.CronTimerSettings(null)
			;
}
