namespace Envelope.ServiceBus.Jobs;

public class JobExecuteResult
{
	public Guid ExecutionId { get; }
	public bool Continue { get; set; }
	public JobExecuteStatus ExecuteStatus { get; set; }

	public JobExecuteResult(bool @continue)
		: this(Guid.NewGuid(), @continue, JobExecuteStatus.Running)
	{
	}

	internal JobExecuteResult(Guid executionId, bool @continue)
		: this(executionId, @continue, JobExecuteStatus.Running)
	{
	}

	public JobExecuteResult(bool @continue, JobExecuteStatus status)
		: this(Guid.NewGuid(), @continue, status)
	{
	}

	internal JobExecuteResult(Guid executionId, bool @continue, JobExecuteStatus status)
	{
		ExecutionId = executionId;
		Continue = @continue;
		ExecuteStatus = status;
	}

	public JobExecuteResult SetStatus(JobExecuteStatus? newStatus, bool force = false)
	{
		if (newStatus.HasValue && (force || (int)ExecuteStatus < (int)newStatus))
			ExecuteStatus = newStatus.Value;

		return this;
	}
}
