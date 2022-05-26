using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.DistributedCoordinator;
using Envelope.ServiceBus.DistributedCoordinator.Internal;
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.ServiceBus.Orchestrations.Persistence.Internal;

namespace Envelope.ServiceBus.Orchestrations.Configuration;

public interface IOrchestrationHostConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IOrchestrationHostConfigurationBuilder<TBuilder, TObject>
	where TObject : IOrchestrationHostConfiguration
{
	TBuilder Object(TObject orchestrationHostConfiguration);

	TObject Build(bool finalize = false);

	TBuilder HostName(string hostName, bool force = false);

	TBuilder RegisterAsHostedService(bool asHostedService);

	TBuilder OrchestrationRepositoryFactory(Func<IServiceProvider, IOrchestrationRepository> orchestrationRepositoryFactory, bool force = false);

	TBuilder DistributedLockProviderFactory(Func<IServiceProvider, IDistributedLockProvider> distributedLockProviderFactory, bool force = false);

	TBuilder EventPublisherFactory(Func<IServiceProvider, IEventPublisher> eventPublisherFactory, bool force = false);

	TBuilder ErrorHandlerConfigurationBuilder(ErrorHandlerConfigurationBuilder errorHandlerConfigurationBuilder, bool force = false);
}

public abstract class OrchestrationHostConfigurationBuilderBase<TBuilder, TObject> : IOrchestrationHostConfigurationBuilder<TBuilder, TObject>
	where TBuilder : OrchestrationHostConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IOrchestrationHostConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _orchestrationHostConfiguration;

	protected OrchestrationHostConfigurationBuilderBase(TObject orchestrationHostConfiguration)
	{
		_orchestrationHostConfiguration = orchestrationHostConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject orchestrationHostConfiguration)
	{
		_orchestrationHostConfiguration = orchestrationHostConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _orchestrationHostConfiguration.Validate(nameof(IOrchestrationHostConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _orchestrationHostConfiguration;
	}

	public TBuilder HostName(string hostName, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_orchestrationHostConfiguration.HostName))
			_orchestrationHostConfiguration.HostName = hostName;

		return _builder;
	}

	public TBuilder RegisterAsHostedService(bool asHostedService)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_orchestrationHostConfiguration.RegisterAsHostedService = asHostedService;
		return _builder;
	}

	public TBuilder OrchestrationRepositoryFactory(Func<IServiceProvider, IOrchestrationRepository> orchestrationRepositoryFactory, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _orchestrationHostConfiguration.OrchestrationRepositoryFactory == null)
			_orchestrationHostConfiguration.OrchestrationRepositoryFactory = orchestrationRepositoryFactory;

		return _builder;
	}

	public TBuilder DistributedLockProviderFactory(Func<IServiceProvider, IDistributedLockProvider> distributedLockProviderFactory, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _orchestrationHostConfiguration.DistributedLockProviderFactory == null)
			_orchestrationHostConfiguration.DistributedLockProviderFactory = distributedLockProviderFactory;

		return _builder;
	}

	public TBuilder EventPublisherFactory(Func<IServiceProvider, IEventPublisher> eventPublisherFactory, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _orchestrationHostConfiguration.EventPublisherFactory == null)
			_orchestrationHostConfiguration.EventPublisherFactory = eventPublisherFactory;

		return _builder;
	}

	public TBuilder ErrorHandlerConfigurationBuilder(ErrorHandlerConfigurationBuilder errorHandlerConfigurationBuilder, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _orchestrationHostConfiguration.ErrorHandlerConfigurationBuilder == null)
			_orchestrationHostConfiguration.ErrorHandlerConfigurationBuilder = errorHandlerConfigurationBuilder;

		return _builder;
	}
}

public class OrchestrationHostConfigurationBuilder : OrchestrationHostConfigurationBuilderBase<OrchestrationHostConfigurationBuilder, IOrchestrationHostConfiguration>
{
	public OrchestrationHostConfigurationBuilder()
		: base(new OrchestrationHostConfiguration())
	{
	}

	public OrchestrationHostConfigurationBuilder(OrchestrationHostConfiguration orchestrationHostConfiguration)
		: base(orchestrationHostConfiguration)
	{
	}

	public static implicit operator OrchestrationHostConfiguration?(OrchestrationHostConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._orchestrationHostConfiguration as OrchestrationHostConfiguration;
	}

	public static implicit operator OrchestrationHostConfigurationBuilder?(OrchestrationHostConfiguration orchestrationHostConfiguration)
	{
		if (orchestrationHostConfiguration == null)
			return null;

		return new OrchestrationHostConfigurationBuilder(orchestrationHostConfiguration);
	}

	internal static OrchestrationHostConfigurationBuilder GetDefaultBuilder()
		=> new OrchestrationHostConfigurationBuilder()
			.OrchestrationRepositoryFactory(sp => new InMemoryOrchestrationRepository())
			.DistributedLockProviderFactory(sp => new InMemoryLockProvider())
			//.EventPublisherFactory(sp => sp.GetRequiredService<IEventPublisher>())
			.ErrorHandlerConfigurationBuilder(Envelope.ServiceBus.Configuration.ErrorHandlerConfigurationBuilder.GetDefaultBuilder());
}
