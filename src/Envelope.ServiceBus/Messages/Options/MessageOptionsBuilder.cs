using Envelope.Exceptions;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.Transactions;
using System.Text;

namespace Envelope.ServiceBus.Messages.Options;

public interface IMessageOptionsBuilder<TBuilder, TObject>
	where TBuilder : IMessageOptionsBuilder<TBuilder, TObject>
	where TObject : IMessageOptions
{
	TBuilder Object(TObject messageOptions);

	TObject Build(bool finalize = false);

	TBuilder TransactionController(ITransactionController transactionController, bool force = true);

	TBuilder ExchangeQueueName(string exchangeQueueName, bool force = true);

	TBuilder DisabledMessagePersistence(bool disabledMessagePersistence);

	TBuilder IdSession(Guid? idSession, bool force = true);

	TBuilder ContentType(string contentType, bool force = true);

	TBuilder ContentEncoding(Encoding? contentEncoding, bool force = true);

	TBuilder RoutingKey(string routingKey, bool force = true);

	TBuilder IsAsynchronousInvocation(bool isAsynchronousInvocation);

	TBuilder ErrorHandling(IErrorHandlingController? errorHandling, bool force = true);

	TBuilder Headers(IMessageHeaders? headers, bool force = true);

	TBuilder Timeout(TimeSpan? timeout, bool force = true);

	TBuilder IsCompressContent(bool isCompressContent);

	TBuilder IsEncryptContent(bool isEncryptContent);

	TBuilder Priority(int priority, bool force = true);

	TBuilder DisableFaultQueue(bool disableFaultQueue);

	TBuilder ThrowNoHandlerException(bool throwNoHandlerException, bool force = true);
}

public abstract class MessageOptionsBuilderBase<TBuilder, TObject> : IMessageOptionsBuilder<TBuilder, TObject>
	where TBuilder : MessageOptionsBuilderBase<TBuilder, TObject>
	where TObject : IMessageOptions
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _messageOptions;

	protected MessageOptionsBuilderBase(TObject messageOptions)
	{
		_messageOptions = messageOptions;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject messageOptions)
	{
		_messageOptions = messageOptions;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _messageOptions.Validate(nameof(IMessageOptions));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		return _messageOptions;
	}

	public TBuilder TransactionController(ITransactionController transactionController, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageOptions.TransactionController == null)
			_messageOptions.TransactionController = transactionController;

		return _builder;
	}

	public TBuilder ExchangeQueueName(string exchangeQueueName, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_messageOptions.ExchangeName))
			_messageOptions.ExchangeName = exchangeQueueName;

		return _builder;
	}

	public TBuilder DisabledMessagePersistence(bool disabledMessagePersistence)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.DisabledMessagePersistence = disabledMessagePersistence;
		return _builder;
	}

	public TBuilder IdSession(Guid? idSession, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_messageOptions.IdSession.HasValue)
			_messageOptions.IdSession = idSession;

		return _builder;
	}

	public TBuilder ContentType(string contentType, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_messageOptions.ContentType))
			_messageOptions.ContentType = contentType;

		return _builder;
	}

	public TBuilder ContentEncoding(Encoding? contentEncoding, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageOptions.ContentEncoding == null)
			_messageOptions.ContentEncoding = contentEncoding;

		return _builder;
	}

	public TBuilder RoutingKey(string routingKey, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_messageOptions.RoutingKey))
			_messageOptions.RoutingKey = routingKey;

		return _builder;
	}

	public TBuilder IsAsynchronousInvocation(bool isAsynchronousInvocation)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.IsAsynchronousInvocation = isAsynchronousInvocation;
		return _builder;
	}

	public TBuilder ErrorHandling(IErrorHandlingController? errorHandling, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageOptions.ErrorHandling == null)
			_messageOptions.ErrorHandling = errorHandling;

		return _builder;
	}

	public TBuilder Headers(IMessageHeaders? headers, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || _messageOptions.Headers == null)
			_messageOptions.Headers = headers;

		return _builder;
	}

	public TBuilder Timeout(TimeSpan? timeout, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_messageOptions.Timeout.HasValue)
			_messageOptions.Timeout = timeout;

		return _builder;
	}

	public TBuilder IsCompressContent(bool isCompressContent)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.IsCompressContent = isCompressContent;
		return _builder;
	}

	public TBuilder IsEncryptContent(bool isEncryptContent)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.IsEncryptContent = isEncryptContent;
		return _builder;
	}

	public TBuilder Priority(int priority, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.Priority = priority;
		return _builder;
	}

	public TBuilder DisableFaultQueue(bool disableFaultQueue)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_messageOptions.DisableFaultQueue = disableFaultQueue;
		return _builder;
	}

	public TBuilder ThrowNoHandlerException(bool throwNoHandlerException, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || !_messageOptions.ThrowNoHandlerException.HasValue)
			_messageOptions.ThrowNoHandlerException = throwNoHandlerException;

		return _builder;
	}
}

public class MessageOptionsBuilder : MessageOptionsBuilderBase<MessageOptionsBuilder, IMessageOptions>
{
	public MessageOptionsBuilder()
		: base(new MessageOptions())
	{
	}

	public MessageOptionsBuilder(MessageOptions options)
		: base(options)
	{
	}

	public static implicit operator MessageOptions?(MessageOptionsBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._messageOptions as MessageOptions;
	}

	public static implicit operator MessageOptionsBuilder?(MessageOptions options)
	{
		if (options == null)
			return null;

		return new MessageOptionsBuilder(options);
	}

	internal static MessageOptionsBuilder GetDefaultBuilder<TMessage>()
		where TMessage : class, IMessage
		=> GetDefaultBuilder(typeof(TMessage));

	internal static MessageOptionsBuilder GetDefaultBuilder(Type messageType)
		=> messageType != null
		? new MessageOptionsBuilder()
			.ExchangeQueueName(messageType.FullName!)
			.ContentType(messageType.FullName!)
			.RoutingKey(messageType.FullName!)
			.IsAsynchronousInvocation(true)
			//.ContentEncoding(null)
			//.TransactionController(null)
			//.IdSession(null)
			//.ErrorHandling(null)
			//.Headers(null)
			//.Timeout(null)
			//.IsCompressContent(false)
			//.IsEncryptContent(false)
			//.Priority(0)
			.DisabledMessagePersistence(true)
			.DisableFaultQueue(true)
		: throw new ArgumentNullException(nameof(messageType));
}