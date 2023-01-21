namespace Envelope.ServiceBus.Jobs;

public static class LogCodes
{
	public const string STARTING = "job_starting";
	public const string STARTING_ERROR = "job_starting_error";
	public const string STARTED = "job_started";
	public const string STOPPING = "job_stopping";
	public const string STOPPING_ERROR = "job_stopping_error";
	public const string STOPPED = "job_stopped";
	public const string GLOBAL_ERROR = "job_global_error";
	public const string LOAD_DATA_ERROR = "job_load_data_error";
	public const string SAVE_DATA_ERROR = "job_save_data_error";
}
