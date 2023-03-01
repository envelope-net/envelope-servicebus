namespace Envelope.ServiceBus.Queries;

public interface IServiceBusReader : IJobMessageReader, IDisposable, IAsyncDisposable
{
	Task<List<IDbHost>> GetHostsAsync(CancellationToken cancellationToken = default);

	Task<List<IDbHostLog>> GetHostLogsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default);

	Task<List<IDbJob>> GetJobsAsync(Guid hostInstanceId, CancellationToken cancellationToken = default);

	Task<List<IDbJob>> GetJobsAsync(string jobName, string hostName, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default);

	Task<IDbJob?> GetJobAsync(Guid jobInstanceId, CancellationToken cancellationToken = default);

	Task<List<IDbJobExecution>> GetJobLatestExecutionsAsync(Guid jobInstanceId, int page = 1, int pageSize = 3, CancellationToken cancellationToken = default);

	Task<List<IDbJobExecution>> GetJobExecutionsAsync(Guid jobInstanceId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

	Task<IDbJobExecution?> GetJobExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

	Task<List<IDbJobLog>> GetJobLogsAsync(Guid executionId, CancellationToken cancellationToken = default);

	Task<List<IDbJobLog>> JobLogsForMessageAsync(Guid jobMessageId, CancellationToken cancellationToken = default);

	Task<IDbJobLog?> GetJobLogAsync(Guid idLogMessage, CancellationToken cancellationToken = default);

	Task<List<IDbJobLog>> JobLogsForCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
