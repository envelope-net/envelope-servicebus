using Envelope.ServiceBus.Jobs.Configuration;
using Envelope.Trace;

namespace Envelope.ServiceBus.Jobs.Internal;

internal class JobController : IJobController
{
	private readonly IJobRegister _jobRegister;
	private readonly IServiceProvider _serviceProvider;
	private readonly IJobProviderConfiguration _config;

	public JobController(IServiceProvider serviceProvider, IJobProviderConfiguration config)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_jobRegister = config.JobRegisterInternal ?? throw new InvalidOperationException($"{nameof(config.JobRegisterInternal)} == null");
	}

	public Task StartJobAsync(ITraceInfo traceInfo, string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if (!_jobRegister.JobsInternal.TryGetValue(name, out var job))
			throw new InvalidOperationException($"No job with name {name} found");

		return job.StartAsync(traceInfo);
	}

	public Task StopJobAsync(ITraceInfo traceInfo, string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if (!_jobRegister.JobsInternal.TryGetValue(name, out var job))
			throw new InvalidOperationException($"No job with name {name} found");

		return job.StopAsync(traceInfo);
	}

	async Task IJobController.StartAllInternalAsync(ITraceInfo traceInfo)
	{
		foreach (var job in _jobRegister.JobsInternal.Values.Where(x => x.Status != JobStatus.Disabled))
		{
			try
			{
				await job.StartAsync(traceInfo).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var executeResult = new JobExecuteResult(job.JobInstanceId, true, JobExecuteStatus.NONE);
				var detail = nameof(IJobController.StartAllInternalAsync);
				await 
					_config
						.JobLogger(_serviceProvider)
						.LogErrorAsync(
							traceInfo,
							job,
							executeResult,
							null,
							LogCodes.STARTING_ERROR,
							x => x.ExceptionInfo(ex).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);
			}
		}
	}

	async Task IJobController.StopAllInternalAsync(ITraceInfo traceInfo)
	{
		foreach (var job in _jobRegister.JobsInternal.Values.Where(x => x.Status != JobStatus.Disabled))
		{
			try
			{
				await job.StopAsync(traceInfo).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var executeResult = new JobExecuteResult(job.JobInstanceId, true, JobExecuteStatus.NONE);
				var detail = nameof(IJobController.StopAllInternalAsync);
				await
					_config
						.JobLogger(_serviceProvider)
						.LogErrorAsync(
							traceInfo,
							job,
							executeResult,
							null,
							LogCodes.STOPPING_ERROR,
							x => x.ExceptionInfo(ex).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);
			}
		}
	}
}
