using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Caching.Memory;

namespace Envelope.ServiceBus.Messages.Internal;

internal class InMemoryMessageBodyProvider : IMessageBodyProvider
{
	private readonly IMemoryCache _cache;
	private readonly TimeSpan _slidingExpiration;

	public InMemoryMessageBodyProvider(TimeSpan slidingExpiration)
	{
		_slidingExpiration = slidingExpiration;
		_cache = new MemoryCache(new MemoryCacheOptions());
	}

	public IResult SaveToStorage<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo traceInfo, ITransactionContext transactionContext)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder();

		if (messagesMetadata != null && message != null)
		{
			foreach (var metadata in messagesMetadata)
			{
				metadata.HasSelfContent = false;
				_cache.Set(metadata.MessageId, message, new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration });
			}
		}

		return result.Build();
	}

	public IResult<Guid> SaveReplyToStorage<TResponse>(Guid messageId, TResponse? response, ITraceInfo traceInfo, ITransactionContext transactionContext)
	{
		var result = new ResultBuilder<Guid>();

		if (response != null)
		{
			_cache.Set($"{messageId}:REPLY", response, new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration });
		}

		return result.WithData(Guid.NewGuid()).Build();
	}

	public Task<IResult> SaveToStorageAsync<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
		where TMessage : class, IMessage
		=> Task.FromResult(SaveToStorage(messagesMetadata, message, traceInfo, transactionContext));

	public Task<IResult<Guid>> SaveReplyToStorageAsync<TResponse>(Guid messageId, TResponse? response, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
		=> Task.FromResult(SaveReplyToStorage(messageId, response, traceInfo, transactionContext));

	public Task<IResult<TMessage?>> LoadFromStorageAsync<TMessage>(IMessageMetadata messageMetadata, ITraceInfo traceInfo, ITransactionContext transactionContext, CancellationToken cancellationToken)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder<TMessage?>();

		if (messageMetadata == null)
			throw new ArgumentNullException(nameof(messageMetadata));

		if (_cache.TryGetValue(messageMetadata.MessageId, out var message))
		{
			if (message is TMessage tMessage)
				return Task.FromResult(result.WithData(tMessage).Build());
			else
				throw new InvalidOperationException($"Message with id {messageMetadata.MessageId} is not type of {typeof(TMessage).FullName}");
		}

		return Task.FromResult(result.WithData(default).Build());
	}

	public bool AllowMessagePersistence(bool disabledMessagePersistence, IMessageMetadata message)
		=> !disabledMessagePersistence;

	public bool AllowAnyMessagePersistence(bool disabledMessagePersistence, IEnumerable<IMessageMetadata> message)
		=> !disabledMessagePersistence;
}
