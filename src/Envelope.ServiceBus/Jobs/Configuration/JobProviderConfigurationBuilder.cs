using Envelope.Exceptions;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs.Configuration.Internal;
using Envelope.ServiceBus.Jobs.Internal;
using Envelope.ServiceBus.Jobs.Logging;
using Envelope.ServiceBus.Queries;
using Envelope.ServiceBus.Queries.Internal;
using Envelope.ServiceBus.Writers;
using Envelope.ServiceBus.Writers.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Jobs.Configuration;

public interface IJobProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IJobProviderConfigurationBuilder<TBuilder, TObject>
	where TObject : IJobProviderConfiguration
{
	TBuilder Object(TObject jobProviderConfiguration);

	TObject Build(IServiceProvider serviceProvider, bool finalize = false);

	internal TBuilder HostInfo(IHostInfo hostInfo, bool force = true);

	TBuilder JobRepository(Func<IServiceProvider, IJobRepository> jobRepository, bool force = true);

	TBuilder JobLogger(Func<IServiceProvider, IJobLogger> jobLogger, bool force = true);

	TBuilder ServiceBusReader(Func<IServiceProvider, IServiceBusReader> serviceBusReader, bool force = true);

	TBuilder JobMessageWriter(Func<IServiceProvider, IJobMessageWriter> jobMessageWriter, bool force = true);

	TBuilder RegisterJob(IJob job);

	TBuilder RegisterJob(string jobName, Func<IServiceProvider, IJob> job);

	//TBuilder RegisterJob<TData>(IJob<TData> job, TData? data, ITraceInfo traceInfo);
}

public abstract class JobProviderConfigurationBuilderBase<TBuilder, TObject> : IJobProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : JobProviderConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IJobProviderConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _jobProviderConfiguration;

	private readonly ConcurrentDictionary<string, RegisteredJob> _jobs = new();

	protected JobProviderConfigurationBuilderBase(TObject jobProviderConfiguration)
	{
		_jobProviderConfiguration = jobProviderConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject jobProviderConfiguration)
	{
		_jobProviderConfiguration = jobProviderConfiguration;
		return _builder;
	}

	public TObject Build(IServiceProvider serviceProvider, bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _jobProviderConfiguration.Validate(nameof(IJobProviderConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		var register = new JobRegister(serviceProvider, _jobProviderConfiguration);
		foreach (var job in _jobs.Values)
			job.RegisterJob(serviceProvider, register);

		_jobProviderConfiguration.JobRegisterInternal = register;

		return _jobProviderConfiguration;
	}

	internal TBuilder HostInfo(IHostInfo hostInfo, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobProviderConfiguration.HostInfoInternal == null)
			_jobProviderConfiguration.HostInfoInternal = hostInfo;

		return _builder;
	}

	TBuilder IJobProviderConfigurationBuilder<TBuilder, TObject>.HostInfo(IHostInfo hostInfo, bool force)
		=> HostInfo(hostInfo, force);

	public TBuilder JobRepository(Func<IServiceProvider, IJobRepository> jobRepository, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobProviderConfiguration.JobRepository == null)
			_jobProviderConfiguration.JobRepository = jobRepository;

		return _builder;
	}

	public TBuilder JobLogger(Func<IServiceProvider, IJobLogger> jobLogger, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobProviderConfiguration.JobLogger == null)
			_jobProviderConfiguration.JobLogger = jobLogger;

		return _builder;
	}

	public TBuilder ServiceBusReader(Func<IServiceProvider, IServiceBusReader> serviceBusReader, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobProviderConfiguration.ServiceBusReader == null)
			_jobProviderConfiguration.ServiceBusReader = serviceBusReader;

		return _builder;
	}

	public TBuilder JobMessageWriter(Func<IServiceProvider, IJobMessageWriter> jobMessageWriter, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _jobProviderConfiguration.JobMessageWriter == null)
			_jobProviderConfiguration.JobMessageWriter = jobMessageWriter;

		return _builder;
	}

	public TBuilder RegisterJob(IJob job)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (job == null)
			throw new ArgumentNullException(nameof(job));

		_jobs.AddOrUpdate(
			job.Name,
			key => new RegisteredJob { Job = job },
			(key, existingJob) => throw new InvalidOperationException($"Job with {nameof(job.Name)} = {job.Name} already registered"));

		return _builder;
	}

	public TBuilder RegisterJob(string jobName, Func<IServiceProvider, IJob> job)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (job == null)
			throw new ArgumentNullException(nameof(job));

		_jobs.AddOrUpdate(
			jobName,
			key => new RegisteredJob { JobFactory = job },
			(key, existingJob) => throw new InvalidOperationException($"Job with {nameof(jobName)} = {jobName} already registered"));

		return _builder;
	}
}

public class JobProviderConfigurationBuilder : JobProviderConfigurationBuilderBase<JobProviderConfigurationBuilder, IJobProviderConfiguration>
{
	public JobProviderConfigurationBuilder()
		: base(new JobProviderConfiguration())
	{
	}

	public static JobProviderConfigurationBuilder GetDefaultBuilder()
		=> new JobProviderConfigurationBuilder()
			.JobRepository(sp => new InMemoryJobRepository())
			.JobLogger(sp => new DefaultJobLogger(sp.GetRequiredService<ILogger<DefaultJobLogger>>()))
			.ServiceBusReader(sp => new DefaultServiceBusReader())
			.JobMessageWriter(sp => new DefaultJobMessageWriter())
			;
}
