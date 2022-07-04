using Envelope.Exceptions;

namespace Envelope.ServiceBus.Configuration;

public interface IErrorHandlerConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IErrorHandlerConfigurationBuilder<TBuilder, TObject>
	where TObject : IErrorHandlerConfiguration
{
	TBuilder Object(TObject errorHandlerConfiguration);

	TObject Build(bool finalize = false);

	TBuilder IterationRetryTable(Dictionary<int, TimeSpan> iterationRetryTable, bool force = true);

	TBuilder DefaultRetryInterval(TimeSpan defaultRetryInterval);

	TBuilder MaxRetryCount(int? maxRetryCount, bool force = true);
}

public abstract class ErrorHandlerConfigurationBuilderBase<TBuilder, TObject> : IErrorHandlerConfigurationBuilder<TBuilder, TObject>
	where TBuilder : ErrorHandlerConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IErrorHandlerConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _errorHandlerConfiguration;

	protected ErrorHandlerConfigurationBuilderBase(TObject errorHandlerConfiguration)
	{
		_errorHandlerConfiguration = errorHandlerConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject errorHandlerConfiguration)
	{
		_errorHandlerConfiguration = errorHandlerConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _errorHandlerConfiguration.Validate(nameof(IErrorHandlerConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _errorHandlerConfiguration;
	}

	public TBuilder IterationRetryTable(Dictionary<int, TimeSpan> iterationRetryTable, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _errorHandlerConfiguration.IterationRetryTable == null)
			_errorHandlerConfiguration.IterationRetryTable = iterationRetryTable;

		return _builder;
	}

	public TBuilder DefaultRetryInterval(TimeSpan defaultRetryInterval)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_errorHandlerConfiguration.DefaultRetryInterval = defaultRetryInterval;

		return _builder;
	}

	public TBuilder MaxRetryCount(int? maxRetryCount, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_errorHandlerConfiguration.MaxRetryCount.HasValue)
			_errorHandlerConfiguration.MaxRetryCount = maxRetryCount;

		return _builder;
	}
}

public class ErrorHandlerConfigurationBuilder : ErrorHandlerConfigurationBuilderBase<ErrorHandlerConfigurationBuilder, IErrorHandlerConfiguration>
{
	public ErrorHandlerConfigurationBuilder()
		: base(new ErrorHandlerConfiguration())
	{
	}

	public ErrorHandlerConfigurationBuilder(ErrorHandlerConfiguration errorHandlerConfiguration)
		: base(errorHandlerConfiguration)
	{
	}

	public static implicit operator ErrorHandlerConfiguration?(ErrorHandlerConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._errorHandlerConfiguration as ErrorHandlerConfiguration;
	}

	public static implicit operator ErrorHandlerConfigurationBuilder?(ErrorHandlerConfiguration errorHandlerConfiguration)
	{
		if (errorHandlerConfiguration == null)
			return null;

		return new ErrorHandlerConfigurationBuilder(errorHandlerConfiguration);
	}

	internal static ErrorHandlerConfigurationBuilder GetDefaultBuilder()
		=> new()
			//.IterationRetryTable(null)
			//.DefaultRetryInterval(TimeSpan.FromSeconds(300))
			//.MaxRetryCount(null)
			;
}
