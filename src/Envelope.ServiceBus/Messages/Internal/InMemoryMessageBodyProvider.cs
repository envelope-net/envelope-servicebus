using Envelope.Services;
using Envelope.Trace;
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

	public Task<IResult<Guid>> SaveToStorageAsync<TMessage>(List<IMessageMetadata> messagesMetadata, TMessage? message, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder<Guid>();

		if (messagesMetadata != null && message != null)
		{
			foreach (var metadata in messagesMetadata)
				_cache.Set(metadata.MessageId, message, new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration });
		}

		return Task.FromResult((IResult<Guid>)result.Build());
	}

	public Task<IResult<Guid, Guid>> SaveReplyToStorageAsync<TResponse>(Guid messageId, TResponse? response, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<Guid, Guid>();

		if (response != null)
		{
			_cache.Set($"{messageId}:REPLY", response, new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration });
		}

		return Task.FromResult(result.WithData(Guid.NewGuid()).Build());
	}

	public Task<IResult<TMessage?, Guid>> LoadFromStorageAsync<TMessage>(IMessageMetadata messageMetadata, ITraceInfo<Guid> traceInfo, CancellationToken cancellationToken)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder<TMessage?, Guid>();

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
}
