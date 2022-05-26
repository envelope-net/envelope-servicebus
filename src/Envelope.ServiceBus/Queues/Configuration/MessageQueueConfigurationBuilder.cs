using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Queues.Configuration;

public interface IMessageQueueConfigurationBuilder<TBuilder, TObject, TMessage>
	where TBuilder : IMessageQueueConfigurationBuilder<TBuilder, TObject, TMessage>
	where TObject : IMessageQueueConfiguration<TMessage>
	where TMessage : class, IMessage
{
	TBuilder Object(TObject messageQueueConfiguration);

	TObject Build(bool finalize = false);

	TBuilder QueueName(string queueName, bool force = false);

	TBuilder QueueType(QueueType queueType);

	TBuilder IsPull(bool isPull);

	TBuilder StartDelay(TimeSpan? startDelay);

	TBuilder FetchInterval(TimeSpan fetchInterval);

	TBuilder MaxSize(int? maxSize, bool force = false);

	TBuilder DefaultProcessingTimeout(TimeSpan? defaultProcessingTimeout, bool force = false);

	TBuilder MessageBodyProvider(IMessageBodyProvider messageBodyProvider, bool force = false);

	TBuilder MessageHandler(HandleMessage<TMessage>? messageHandler, bool force = false);
	
	TBuilder ErrorHandling(IErrorHandlingController? errorHandling, bool force = false);
}

public abstract class MessageQueueConfigurationBuilderBase<TBuilder, TObject, TMessage> : IMessageQueueConfigurationBuilder<TBuilder, TObject, TMessage>
	where TBuilder : MessageQueueConfigurationBuilderBase<TBuilder, TObject, TMessage>
	where TObject : IMessageQueueConfiguration<TMessage>
	where TMessage : class, IMessage
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _messageQueueConfiguration;

	protected MessageQueueConfigurationBuilderBase(TObject messageQueueConfiguration)
	{
		_messageQueueConfiguration = messageQueueConfiguration;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject messageQueueConfiguration)
	{
		_messageQueueConfiguration = messageQueueConfiguration;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _messageQueueConfiguration.Validate(nameof(IMessageQueueConfiguration<TMessage>))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _messageQueueConfiguration;
	}

	public TBuilder QueueName(string queueName, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_messageQueueConfiguration.QueueName))
			_messageQueueConfiguration.QueueName = queueName;

		return _builder;
	}

	public TBuilder QueueType(QueueType queueType)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageQueueConfiguration.QueueType = queueType;
		return _builder;
	}

	public TBuilder IsPull(bool isPull)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageQueueConfiguration.IsPull = isPull;
		return _builder;
	}

	public TBuilder StartDelay(TimeSpan? startDelay)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageQueueConfiguration.StartDelay = startDelay;
		return _builder;
	}

	public TBuilder FetchInterval(TimeSpan fetchInterval)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageQueueConfiguration.FetchInterval = fetchInterval;
		return _builder;
	}

	public TBuilder MaxSize(int? maxSize, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_messageQueueConfiguration.MaxSize.HasValue)
			_messageQueueConfiguration.MaxSize = maxSize;

		return _builder;
	}

	public TBuilder DefaultProcessingTimeout(TimeSpan? defaultProcessingTimeout, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_messageQueueConfiguration.DefaultProcessingTimeout.HasValue)
			_messageQueueConfiguration.DefaultProcessingTimeout = defaultProcessingTimeout;

		return _builder;
	}

	public TBuilder MessageBodyProvider(IMessageBodyProvider messageBodyProvider, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageQueueConfiguration.MessageBodyProvider == null)
			_messageQueueConfiguration.MessageBodyProvider = messageBodyProvider;

		return _builder;
	}

	public TBuilder MessageHandler(HandleMessage<TMessage>? messageHandler, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageQueueConfiguration.MessageHandler == null)
			_messageQueueConfiguration.MessageHandler = messageHandler;

		return _builder;
	}

	public TBuilder ErrorHandling(IErrorHandlingController? errorHandling, bool force = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageQueueConfiguration.ErrorHandling == null)
			_messageQueueConfiguration.ErrorHandling = errorHandling;

		return _builder;
	}
}

public class MessageQueueConfigurationBuilder<TMessage> : MessageQueueConfigurationBuilderBase<MessageQueueConfigurationBuilder<TMessage>, IMessageQueueConfiguration<TMessage>, TMessage>
		where TMessage : class, IMessage
{
	public MessageQueueConfigurationBuilder(IServiceBusOptions serviceBusOptions)
		: base(new MessageQueueConfiguration<TMessage>(serviceBusOptions))
	{
	}

	public MessageQueueConfigurationBuilder(MessageQueueConfiguration<TMessage> messageQueueConfiguration)
		: base(messageQueueConfiguration)
	{
	}

	public static implicit operator MessageQueueConfiguration<TMessage>?(MessageQueueConfigurationBuilder<TMessage> builder)
	{
		if (builder == null)
			return null;

		return builder._messageQueueConfiguration as MessageQueueConfiguration<TMessage>;
	}

	public static implicit operator MessageQueueConfigurationBuilder<TMessage>?(MessageQueueConfiguration<TMessage> messageQueueConfiguration)
	{
		if (messageQueueConfiguration == null)
			return null;

		return new MessageQueueConfigurationBuilder<TMessage>(messageQueueConfiguration);
	}

	internal static MessageQueueConfigurationBuilder<TMessage> GetDefaultBuilder(IServiceBusOptions serviceBusOptions, HandleMessage<TMessage>? messageHandler)
	{
		var result =
			new MessageQueueConfigurationBuilder<TMessage>(serviceBusOptions)
				.QueueName(typeof(TMessage).FullName!)
				.QueueType(Queues.QueueType.Sequential_Delayable)
				//.IsPull(false)
				//.StartDelay(null)
				//.MaxSize(null)
				//.DefaultProcessingTimeout(null)
				//.ErrorHandling(null)
				.FetchInterval(TimeSpan.FromMilliseconds(1))
				.MessageBodyProvider(new InMemoryMessageBodyProvider(TimeSpan.FromMinutes(1)))
				.MessageHandler(messageHandler);

		return result;
	}
}
