using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Orchestrations.EventHandlers.Internal;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues.Internal;

namespace Envelope.ServiceBus.Queues.Configuration;

public interface IQueueProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : IQueueProviderConfigurationBuilder<TBuilder, TObject>
	where TObject : IQueueProviderConfiguration
{
	TBuilder Object(TObject queueProviderConfiguration);

	TObject Build(bool finalize = false);

	TBuilder FaultQueue(Func<IServiceProvider, IFaultQueue> faultQueue, bool force = false);

	TBuilder RegisterDefaultQueue<TMessage>(HandleMessage<TMessage>? messageHandler, bool force = false)
		where TMessage : class, IMessage;

	TBuilder RegisterQueue<TMessage>(string queueName, Func<IServiceProvider, IMessageQueue<TMessage>> messageQueue, bool force = false)
		where TMessage : class, IMessage;
}

public abstract class QueueProviderConfigurationBuilderBase<TBuilder, TObject> : IQueueProviderConfigurationBuilder<TBuilder, TObject>
	where TBuilder : QueueProviderConfigurationBuilderBase<TBuilder, TObject>
	where TObject : IQueueProviderConfiguration
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _queueProviderConfiguration;

	protected QueueProviderConfigurationBuilderBase(TObject queueProviderConfiguration)
	{
		_queueProviderConfiguration = queueProviderConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject queueProviderConfiguration)
	{
		_queueProviderConfiguration = queueProviderConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _queueProviderConfiguration.Validate(nameof(IQueueProviderConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _queueProviderConfiguration;
	}

	public TBuilder FaultQueue(Func<IServiceProvider, IFaultQueue> faultQueue, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _queueProviderConfiguration.FaultQueue == null)
			_queueProviderConfiguration.FaultQueue = faultQueue;

		return _builder;
	}

	public TBuilder RegisterDefaultQueue<TMessage>(HandleMessage<TMessage>? messageHandler, bool force = false)
		where TMessage : class, IMessage
		=> RegisterQueue(
			typeof(TMessage).FullName!,
			sp =>
				new InMemoryMessageQueue<TMessage>(MessageQueueConfigurationBuilder<TMessage>
					.GetDefaultBuilder(_queueProviderConfiguration.ServiceBusOptions, messageHandler)
					.Build()),
			force);

	public TBuilder RegisterQueue<TMessage>(string queueName, Func<IServiceProvider, IMessageQueue<TMessage>> messageQueue, bool force = false)
		where TMessage : class, IMessage
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force)
			_queueProviderConfiguration.MessageQueues[queueName] = messageQueue;
		else
			_queueProviderConfiguration.MessageQueues.TryAdd(queueName, messageQueue);

		return _builder;
	}
}

public class QueueProviderConfigurationBuilder : QueueProviderConfigurationBuilderBase<QueueProviderConfigurationBuilder, IQueueProviderConfiguration>
{
	public QueueProviderConfigurationBuilder(IServiceBusOptions serviceBusOptions)
		: base(new QueueProviderConfiguration(serviceBusOptions))
	{
	}

	private QueueProviderConfigurationBuilder(QueueProviderConfiguration queueProviderConfiguration)
		: base(queueProviderConfiguration)
	{
	}

	public static implicit operator QueueProviderConfiguration?(QueueProviderConfigurationBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._queueProviderConfiguration as QueueProviderConfiguration;
	}

	public static implicit operator QueueProviderConfigurationBuilder?(QueueProviderConfiguration queueProviderConfiguration)
	{
		if (queueProviderConfiguration == null)
			return null;

		return new QueueProviderConfigurationBuilder(queueProviderConfiguration);
	}

	internal static QueueProviderConfigurationBuilder GetDefaultBuilder(IServiceBusOptions serviceBusOptions)
		=> new QueueProviderConfigurationBuilder(serviceBusOptions)
			.RegisterDefaultQueue<OrchestrationEvent>(OrchestrationEventHandler.HandleMessageAsync, true)
			.FaultQueue(sp => new DroppingFaultQueue());
}
