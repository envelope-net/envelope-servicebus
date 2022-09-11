using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Trace;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.Queues.Internal;

internal class QueueProvider : IQueueProvider
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ConcurrentDictionary<string, IMessageQueue> _cache;
	private readonly IQueueProviderConfiguration _config;

	public IFaultQueue FaultQueue { get; }

	internal ServiceBusOptions ServiceBusOptions { get; }

	public QueueProvider(IServiceProvider serviceProvider, IQueueProviderConfiguration configuration, ServiceBusOptions serviceBusOptions)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(configuration));
		_config = configuration ?? throw new ArgumentNullException(nameof(configuration));
		FaultQueue = _config.FaultQueue(_serviceProvider);
		_cache = new ConcurrentDictionary<string, IMessageQueue>();
	}

	public List<IMessageQueue> GetAllQueues()
		=> _cache.Values.ToList();

	public IMessageQueue? GetQueue(string queueName)
	{
		if (string.IsNullOrWhiteSpace(queueName))
			return null;

		var queue = _cache.GetOrAdd(queueName, queueName =>
		{
			if (_config.MessageQueuesInternal.TryGetValue(queueName, out var queueFactory))
			{
				var queue = queueFactory(_serviceProvider);
				if (queue == null)
					throw new InvalidOperationException($"QueueFactory with name {queueName} cannot create message queue");

				return queue;
			}

			throw new InvalidOperationException($"No queue with name {queueName} was registered.");
		});

		return queue;
	}

	public IMessageQueue<TMessage>? GetQueue<TMessage>(string queueName)
		where TMessage : class, IMessage
	{
		var queue = GetQueue(queueName);

		if (queue is IMessageQueue<TMessage> messageQueue)
			return messageQueue;
		else
			throw new InvalidOperationException($"Queue with name {queueName} cannot store message type {typeof(TMessage).FullName}");
	}

	public IQueueEnqueueContext CreateQueueEnqueueContext<TMessage>(ITraceInfo traceInfo, IExchangeMessage<TMessage> exchangeMessage)
		where TMessage : class, IMessage
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));

		if (exchangeMessage == null)
			throw new ArgumentNullException(nameof(exchangeMessage));

		var ctx = new QueueEnqueueContext
		{
			MessageId = exchangeMessage.MessageId,
			ParentMessageId = exchangeMessage.ParentMessageId,
			SourceExchangeName = exchangeMessage.ExchangeName,
			PublisherId = exchangeMessage.PublisherId,
			TraceInfo = traceInfo,
			DisabledMessagePersistence = exchangeMessage.DisabledMessagePersistence,
			IdSession = exchangeMessage.IdSession,
			ContentType = exchangeMessage.ContentType,
			ContentEncoding = exchangeMessage.ContentEncoding,
			RoutingKey = exchangeMessage.RoutingKey,
			ErrorHandling = exchangeMessage.ErrorHandling,
			Headers = MessageHeaders.Create(exchangeMessage.Headers),
			IsAsynchronousInvocation = exchangeMessage.IsAsynchronousInvocation,
			Timeout = exchangeMessage.Timeout,
			IsCompressedContent = exchangeMessage.IsCompressedContent,
			IsEncryptedContent = exchangeMessage.IsEncryptedContent,
			Priority = exchangeMessage.Priority,
			DisableFaultQueue = exchangeMessage.DisableFaultQueue
		};

		return ctx;
	}

	public IFaultQueueContext CreateFaultQueueContext<TMessage>(ITraceInfo traceInfo, IExchangeMessage<TMessage> exchangeMessage)
		where TMessage : class, IMessage
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));
		if (exchangeMessage == null)
			throw new ArgumentNullException(nameof(exchangeMessage));

		var ctx = new FaultQueueContext
		{
		};

		return ctx;
	}

	public IFaultQueueContext CreateFaultQueueContext<TMessage>(ITraceInfo traceInfo, IQueuedMessage<TMessage> exchangeMessage)
		where TMessage : class, IMessage
	{
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));
		if (exchangeMessage == null)
			throw new ArgumentNullException(nameof(exchangeMessage));

		var ctx = new FaultQueueContext
		{
		};

		return ctx;
	}
}
