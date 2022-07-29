using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Internal;
using Envelope.Services;
using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Exchange.Internal;

internal class ExchangeProvider : IExchangeProvider
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ConcurrentDictionary<string, IExchange> _cache;
	private readonly IExchangeProviderConfiguration _config;

	public IFaultQueue FaultQueue { get; }

	internal ServiceBusOptions ServiceBusOptions { get; }

	public ExchangeProvider(IServiceProvider serviceProvider, IExchangeProviderConfiguration configuration, ServiceBusOptions serviceBusOptions)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(configuration));
		_config = configuration ?? throw new ArgumentNullException(nameof(configuration));
		FaultQueue = _config.FaultQueue(_serviceProvider);
		_cache = new ConcurrentDictionary<string, IExchange>();
	}

	public List<IExchange> GetAllExchanges()
		=> _cache.Values.ToList();

	public IExchange? GetExchange(string exchangeName)
	{
		if (string.IsNullOrWhiteSpace(exchangeName))
			return null;

		var exchange = _cache.GetOrAdd(exchangeName, exchangeName =>
		{
			if (_config.Exchanges.TryGetValue(exchangeName, out var exchangeFactory))
			{
				var exchange = exchangeFactory(_serviceProvider);
				if (exchange == null)
					throw new InvalidOperationException($"ExchangeFactory with name {exchangeName} cannot create an exchange.");

				return exchange;
			}

			throw new InvalidOperationException($"No exchange with name {exchangeName} was registered.");
		});

		return exchange;
	}

	public IExchange<TMessage>? GetExchange<TMessage>(string exchangeName)
		where TMessage : class, IMessage
	{
		var exchange = ((IExchangeProvider)this).GetExchange(exchangeName);

		if (exchange is IExchange<TMessage> messageExchange)
			return messageExchange;
		else
			throw new InvalidOperationException($"Exchange with name {exchangeName} cannot store message type {typeof(TMessage).FullName}");
	}

	public IResult<IExchangeEnqueueContext> CreateExchangeEnqueueContext(ITraceInfo traceInfo, IMessageOptions options, ExchangeType exchangeType, ServiceBusMode serviceBusMode)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<IExchangeEnqueueContext>();

		if (!options.IsAsynchronousInvocation && serviceBusMode == ServiceBusMode.PublishOnly)
			return result.WithInvalidOperationException(traceInfo, $"Cannot call synchronous invocation on {nameof(ServiceBusMode.PublishOnly)} service bus mode");

		if (!options.IsAsynchronousInvocation && exchangeType != ExchangeType.Direct)
			return result.WithInvalidOperationException(traceInfo, $"Cannot call synchronous invocation on non-Direct exchange");

		var context = new ExchangeEnqueueContext
		{
			PublisherId = PublisherHelper.GetPublisherIdentifier(ServiceBusOptions.HostInfo, traceInfo),
			TraceInfo = traceInfo,
			DisabledMessagePersistence = options.DisabledMessagePersistence,
			IdSession = options.IdSession,
			ContentType = options.ContentType,
			ContentEncoding = options.ContentEncoding,
			RoutingKey = options.RoutingKey,
			ErrorHandling = options.ErrorHandling,
			Headers = options.Headers,
			IsAsynchronousInvocation = options.IsAsynchronousInvocation,
			Timeout = options.Timeout,
			IsCompressContent = options.IsCompressContent,
			IsEncryptContent = options.IsEncryptContent,
			Priority = options.Priority,
			DisableFaultQueue = options.DisableFaultQueue
		};

		return result.WithData(context).Build();
	}

	public IFaultQueueContext CreateFaultQueueContext(ITraceInfo traceInfo, IMessageOptions options)
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));
		if (options == null)
			throw new ArgumentNullException(nameof(options));

		var ctx = new FaultQueueContext
		{
		};

		return ctx;
	}
}
