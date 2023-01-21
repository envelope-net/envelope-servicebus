﻿namespace Envelope.ServiceBus.Queries;

public interface IServiceBusQueries : IDisposable, IAsyncDisposable
{
	Task<List<IDbHost>> GetHostsAsync(CancellationToken cancellationToken = default);

	Task<List<IDbHostLog>> GetHostLogsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default);

	Task<List<IDbJob>> GetJobsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default);

	Task<List<IDbJobExecution>> GetJobLatestExecutionsAsync(Guid jobInstanceId, int count = 3, CancellationToken cancellationToken = default);

	Task<List<IDbJobExecution>> GetJobExecutionsAsync(Guid jobInstanceId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

	Task<List<IDbJobLog>> GetJobLogsAsync(Guid executionId, CancellationToken cancellationToken = default);
}