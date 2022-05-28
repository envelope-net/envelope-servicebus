using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Exchange.Internal;

internal class ExchangeMessageFactory<TMessage> : IExchangeMessageFactory<TMessage>
	where TMessage : class, IMessage
{
	public IResult<List<IExchangeMessage<TMessage>>> CreateExchangeMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> exchangeContext,
		ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<List<IExchangeMessage<TMessage>>>();

		if (context == null)
			return result.WithArgumentNullException(traceInfo, nameof(context));

		if (string.IsNullOrWhiteSpace(context.PublisherId))
			return result.WithArgumentException(traceInfo, $"{nameof(context)}.{nameof(context.PublisherId)} == null");

		return exchangeContext.Router.ExchangeType switch
		{
			Routing.ExchangeType.Direct => CreateDirectMessages(message, context, exchangeContext, traceInfo),
			Routing.ExchangeType.Topic => CreateTopicMessages(message, context, exchangeContext, traceInfo),
			Routing.ExchangeType.Headers => CreateHeadersMessages(message, context, exchangeContext, traceInfo),
			_ => CreateFanOutMessages(message, context, exchangeContext, traceInfo),
		};
	}

	private IResult<List<IExchangeMessage<TMessage>>> CreateDirectMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> exchangeContext,
		ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<List<IExchangeMessage<TMessage>>>();

		if (string.IsNullOrWhiteSpace(context.RoutingKey))
			return result.WithInvalidOperationException(traceInfo, $"For the {exchangeContext.Router.ExchangeType} {nameof(Routing.ExchangeType)} the {nameof(context.RoutingKey)} must be set.");

		if (!exchangeContext.Router.Bindings.ContainsKey(context.RoutingKey))
			return result.WithInvalidOperationException(traceInfo, $"{exchangeContext.Router.ExchangeType} Exchange for {nameof(context.RoutingKey)} = {context.RoutingKey} has no binding.");

		var exchangeMessage = new ExchangeMessage<TMessage>
		{
			Processed = false,
			ExchangeName = exchangeContext.ExchangeName,
			MessageId = Guid.NewGuid(),
			ParentMessageId = null,
			PublishingTimeUtc = DateTime.UtcNow,
			PublisherId = context.PublisherId,
			IsAsynchronousInvocation = context.IsAsynchronousInvocation,
			TraceInfo = traceInfo,
			Timeout = context.Timeout,
			IdSession = context.IdSession,
			ContentType = context.ContentType,
			ContentEncoding = context.ContentEncoding,
			IsCompressedContent = context.IsCompressContent,
			IsEncryptedContent = context.IsEncryptContent,
			RoutingKey = context.RoutingKey,
			TargetQueueName = context.RoutingKey,
			ContainsContent = message != null,
			Priority = context.Priority,
			ErrorHandling = context.ErrorHandling,
			Headers = context.Headers?.GetAll(),
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null,
			Message = message,
			DisabledMessagePersistence = context.DisabledMessagePersistence,
			DisableFaultQueue = context.DisableFaultQueue
		};

		return result.WithData(new List<IExchangeMessage<TMessage>> { exchangeMessage }).Build();
	}

	private IResult<List<IExchangeMessage<TMessage>>> CreateTopicMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> exchangeContext,
		ITraceInfo traceInfo)
		=> CreateDirectMessages(message, context, exchangeContext, traceInfo); //TODO: prepis metodu na vyhladanie TargetQueueName podla wildcard (*, ?) chars v RoutingKey

	private IResult<List<IExchangeMessage<TMessage>>> CreateHeadersMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> config,
		ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<List<IExchangeMessage<TMessage>>>();

		var messages = new List<IExchangeMessage<TMessage>>();
		var parentMessageId = Guid.NewGuid();

		if (!config.Router.MatcheHeaders(context.Headers?.GetAll()))
			return result.WithInvalidOperationException(traceInfo, $"{nameof(Routing.ExchangeType.Headers)} Exchange does not match.");

		foreach (var binding in config.Router.Bindings)
		{
			var nowUtc = DateTime.UtcNow;
			var exchangeMessage = new ExchangeMessage<TMessage>
			{
				Processed = false,
				ExchangeName = config.ExchangeName,
				MessageId = Guid.NewGuid(),
				ParentMessageId = config.Router.Bindings.Count == 1 ? null : parentMessageId,
				PublishingTimeUtc = nowUtc,
				PublisherId = context.PublisherId,
				IsAsynchronousInvocation = context.IsAsynchronousInvocation,
				TraceInfo = traceInfo,
				Timeout = context.Timeout,
				IdSession = context.IdSession,
				ContentType = context.ContentType,
				ContentEncoding = context.ContentEncoding,
				IsCompressedContent = context.IsCompressContent,
				IsEncryptedContent = context.IsEncryptContent,
				RoutingKey = context.RoutingKey,
				TargetQueueName = binding.Key,
				ContainsContent = message != null,
				Priority = context.Priority,
				ErrorHandling = context.ErrorHandling,
				Headers = context.Headers?.GetAll(),
				MessageStatus = MessageStatus.Created,
				DelayedToUtc = null,
				Message = message,
				DisabledMessagePersistence = context.DisabledMessagePersistence,
				DisableFaultQueue = context.DisableFaultQueue
			};

			messages.Add(exchangeMessage);
		}

		return result.WithData(messages).Build();
	}

	private IResult<List<IExchangeMessage<TMessage>>> CreateFanOutMessages(
		TMessage? message,
		IExchangeEnqueueContext context,
		ExchangeContext<TMessage> exchangeContext,
		ITraceInfo traceInfo)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<List<IExchangeMessage<TMessage>>>();

		var messages = new List<IExchangeMessage<TMessage>>();
		var parentMessageId = Guid.NewGuid();

		foreach (var binding in exchangeContext.Router.Bindings)
		{
			var nowUtc = DateTime.UtcNow;
			var exchangeMessage = new ExchangeMessage<TMessage>
			{
				Processed = false,
				ExchangeName = exchangeContext.ExchangeName,
				MessageId = Guid.NewGuid(),
				ParentMessageId = exchangeContext.Router.Bindings.Count == 1 ? null : parentMessageId,
				PublishingTimeUtc = nowUtc,
				PublisherId = context.PublisherId,
				IsAsynchronousInvocation = context.IsAsynchronousInvocation,
				TraceInfo = traceInfo,
				Timeout = context.Timeout,
				IdSession = context.IdSession,
				ContentType = context.ContentType,
				ContentEncoding = context.ContentEncoding,
				IsCompressedContent = context.IsCompressContent,
				IsEncryptedContent = context.IsEncryptContent,
				RoutingKey = context.RoutingKey,
				TargetQueueName = binding.Key,
				ContainsContent = message != null,
				Priority = context.Priority,
				ErrorHandling = context.ErrorHandling,
				Headers = context.Headers?.GetAll(),
				MessageStatus = MessageStatus.Created,
				DelayedToUtc = null,
				Message = message,
				DisabledMessagePersistence = context.DisabledMessagePersistence,
				DisableFaultQueue = context.DisableFaultQueue
			};

			messages.Add(exchangeMessage);
		}

		return result.WithData(messages).Build();
	}
}
