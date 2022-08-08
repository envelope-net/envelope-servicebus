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
		_jobRegister = config.JobRegister ?? throw new InvalidOperationException($"{nameof(config.JobRegister)} == null");
	}

	public Task StartJobAsync(ITraceInfo traceInfo, string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if (!_jobRegister.Jobs.TryGetValue(name, out var job))
			throw new InvalidOperationException($"No job with name {name} found");

		return job.StartAsync(traceInfo);
	}

	public Task StopJobAsync(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if (!_jobRegister.Jobs.TryGetValue(name, out var job))
			throw new InvalidOperationException($"No job with name {name} found");

		return job.StopAsync();
	}

	async Task IJobController.StartAllAsync(ITraceInfo traceInfo)
	{
		foreach (var job in _jobRegister.Jobs.Values.Where(x => x.Status != JobStatus.Disabled))
		{
			try
			{
				await job.StartAsync(traceInfo);
			}
			catch (Exception ex)
			{
				var detail = nameof(IJobController.StartAllAsync);
				await 
					_config
						.JobLogger(_serviceProvider)
						.LogErrorAsync(
							traceInfo,
							job.Name,
							x => x.ExceptionInfo(ex).Detail(detail),
							detail,
							null,
							cancellationToken: default);
			}
		}
	}

	async Task IJobController.StopAllAsync(ITraceInfo traceInfo)
	{
		foreach (var job in _jobRegister.Jobs.Values.Where(x => x.Status != JobStatus.Disabled))
		{
			try
			{
				await job.StopAsync();
			}
			catch (Exception ex)
			{
				var detail = nameof(IJobController.StopAllAsync);
				await
					_config
						.JobLogger(_serviceProvider)
						.LogErrorAsync(
							traceInfo,
							job.Name,
							x => x.ExceptionInfo(ex).Detail(detail),
							detail,
							null,
							cancellationToken: default);
			}
		}
	}
}
