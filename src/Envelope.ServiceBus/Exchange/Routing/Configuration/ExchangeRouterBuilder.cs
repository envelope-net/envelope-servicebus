using Envelope.Exceptions;
using Envelope.ServiceBus.Messages;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Exchange.Routing.Configuration;

public interface IExchangeRouterBuilder<TBuilder, TObject>
	where TBuilder : IExchangeRouterBuilder<TBuilder, TObject>
	where TObject : IExhcangeRouter
{
	TBuilder Object(TObject exchangeRouter);

	TObject Build(bool finalize = false);

	TBuilder ExchangeName(string exchangeName, bool force = true);

	TBuilder ExchangeType(ExchangeType exchangeType);

	TBuilder AddDefaultBinding<TMessage>(bool force = true)
		where TMessage : class, IMessage;

	TBuilder AddBinding<TMessage>(string targetQueueName, string routeName, bool force = true)
		where TMessage : class, IMessage;

	TBuilder HeadersMatch(HeadersMatch headersMatch);

	TBuilder AddHeader(string key, object value, bool force = true);
}

public abstract class ExchangeRouterBuilderBase<TBuilder, TObject> : IExchangeRouterBuilder<TBuilder, TObject>
	where TBuilder : ExchangeRouterBuilderBase<TBuilder, TObject>
	where TObject : IExhcangeRouter
{
	private bool _finalized = false;
	protected readonly TBuilder _builder;
	protected TObject _exchangeRouter;

	protected ExchangeRouterBuilderBase(TObject exchangeRouter)
	{
		_exchangeRouter = exchangeRouter;
		_builder = (TBuilder)this;
	}

	public virtual TBuilder Object(TObject exchangeRouter)
	{
		_exchangeRouter = exchangeRouter;
		return _builder;
	}

	public TObject Build(bool finalize = false)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_finalized = finalize;

		var error = _exchangeRouter.Validate(nameof(IExhcangeRouter))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		return _exchangeRouter;
	}

	public TBuilder ExchangeName(string exchangeName, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force || string.IsNullOrWhiteSpace(_exchangeRouter.ExchangeName))
			_exchangeRouter.ExchangeName = exchangeName;

		return _builder;
	}

	public TBuilder ExchangeType(ExchangeType exchangeType)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_exchangeRouter.ExchangeType = exchangeType;
		return _builder;
	}

	public TBuilder AddDefaultBinding<TMessage>(bool force = true)
		where TMessage : class, IMessage
		=> AddBinding<TMessage>(typeof(TMessage).FullName!, typeof(TMessage).FullName!, force);

	public TBuilder AddBinding<TMessage>(string targetQueueName, string routeName, bool force = true)
		where TMessage : class, IMessage
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (force)
			_exchangeRouter.Bindings[targetQueueName] = routeName;
		else
			_exchangeRouter.Bindings.TryAdd(targetQueueName, routeName);

		return _builder;
	}

	public TBuilder HeadersMatch(HeadersMatch headersMatch)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		_exchangeRouter.HeadersMatch = headersMatch;
		return _builder;
	}

	public TBuilder AddHeader(string key, object value, bool force = true)
	{
		if (_finalized)
			throw new ConfigurationException("The builder was finalized");

		if (string.IsNullOrWhiteSpace(key))
			throw new ArgumentNullException(nameof(key));

		if (_exchangeRouter.Headers == null)
			_exchangeRouter.Headers = new Dictionary<string, object>();

		if (force)
			_exchangeRouter.Headers[key] = value;
		else
			_exchangeRouter.Headers.TryAdd(key, value);

		return _builder;
	}
}

public class ExchangeRouterBuilder : ExchangeRouterBuilderBase<ExchangeRouterBuilder, IExhcangeRouter>
{
	public ExchangeRouterBuilder()
		: base(new ExchangeRouter())
	{
	}

	public ExchangeRouterBuilder(ExchangeRouter exchangeRouter)
		: base(exchangeRouter)
	{
	}

	public static implicit operator ExchangeRouter?(ExchangeRouterBuilder builder)
	{
		if (builder == null)
			return null;

		return builder._exchangeRouter as ExchangeRouter;
	}

	public static implicit operator ExchangeRouterBuilder?(ExchangeRouter exchangeRouter)
	{
		if (exchangeRouter == null)
			return null;

		return new ExchangeRouterBuilder(exchangeRouter);
	}

	internal static ExchangeRouterBuilder GetDefaultBuilder<TMessage>()
		where TMessage : class, IMessage
		=> new ExchangeRouterBuilder()
			.ExchangeName(typeof(TMessage).FullName!)
			.ExchangeType(Routing.ExchangeType.Direct)
			.AddDefaultBinding<TMessage>();
}
