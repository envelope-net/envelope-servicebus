using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Jobs.Internal;

internal class JobRegister : IJobRegister
{
	private readonly ConcurrentDictionary<string, IJob> _jobs = new();

	ConcurrentDictionary<string, IJob> IJobRegister.Jobs => _jobs;

	private readonly IServiceProvider _serviceProvider;
	private readonly IJobProviderConfiguration _config;

	public JobRegister(IServiceProvider serviceProvider, IJobProviderConfiguration config)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_config = config ?? throw new ArgumentNullException(nameof(config));
	}

	public void RegisterJob(IJob job)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		_jobs.AddOrUpdate(
			job.Name,
			key =>
			{
				job.Initialize(_config, _serviceProvider);
				return job;
			},
			(key, existingJob) => throw new InvalidOperationException($"Job with {nameof(job.Name)} = {job.Name} already registered"));
	}

	public Task RegisterJobAsync<TData>(IJob<TData> job, TData? data, ITraceInfo traceInfo)
	{
		if (job == null)
			throw new ArgumentNullException(nameof(job));

		_jobs.AddOrUpdate(
			job.Name,
			key =>
			{
				job.Initialize(_config, _serviceProvider);
				return job;
			},
			(key, existingJob) => throw new InvalidOperationException($"Job with {nameof(job.Name)} = {job.Name} already registered"));

		if (data != null)
			return job.SetDataAsync(traceInfo, data);

		return Task.CompletedTask;
	}
}
