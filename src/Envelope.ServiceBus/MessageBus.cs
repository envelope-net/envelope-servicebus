using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public class MessageBus : IMessageBus
{
	protected IServiceProvider ServiceProvider { get; }
	protected IMessageBusOptions MessageBusOptions { get; }
	protected IMessageHandlerRegistry MessageHandlerRegistry { get; }

	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncMessageHandlerProcessors = new();
	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncVoidMessageHandlerProcessors = new();

	public MessageBus(IServiceProvider serviceProvider, IMessageBusOptions options, IMessageHandlerRegistry messageHandlerRegistry)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		MessageBusOptions = options ?? throw new ArgumentNullException(nameof(options));
		MessageHandlerRegistry = messageHandlerRegistry ?? throw new ArgumentNullException(nameof(messageHandlerRegistry));
	}

	public Task<IResult<Guid, Guid>> SendAsync(
		IRequestMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<Guid, Guid>> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, optionsBuilder, TraceInfo<Guid>.Create(null, MessageBusOptions.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber), cancellationToken);

	public Task<IResult<Guid, Guid>> SendAsync(
		IRequestMessage message,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, (Action<MessageOptionsBuilder>?)null, traceInfo, cancellationToken);

	public async Task<IResult<Guid, Guid>> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			var result = new ResultBuilder<Guid, Guid>();
			return result.WithArgumentNullException(traceInfo, nameof(message));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(message.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		return await SendAsync(message, options, isLocalTransactionContext, traceInfo, cancellationToken);
	}

	protected async Task<IResult<Guid, Guid>> SendAsync(
		IRequestMessage message,
		IMessageOptions options,
		bool isLocalTransactionContext,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<Guid, Guid>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(TraceInfo<Guid>.Create(MessageBusOptions.HostInfo.HostName), nameof(traceInfo));

		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		var transactionContext = options.TransactionContext;
		try
		{
			var requestMessageType = message.GetType();

			var savedMessageResult = await SaveRequestMessageAsync(message, options, traceInfo, cancellationToken);
			if (result.MergeHasError(savedMessageResult))
				return result.Build();

			var savedMessage = savedMessageResult.Data;

			if (savedMessage == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedMessage)} == null | {nameof(requestMessageType)} = {requestMessageType.FullName}");
			if (savedMessage.Message == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedMessage)}.{nameof(savedMessage.Message)} == null | {nameof(requestMessageType)} = {requestMessageType.FullName}");

			var handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

			if (handlerContext == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

			handlerContext.MessageHandlerResultFactory = MessageBusOptions.MessageHandlerResultFactory;
			handlerContext.TransactionContext = transactionContext;
			handlerContext.ServiceProvider = ServiceProvider;
			handlerContext.TraceInfo = traceInfo;
			handlerContext.HostInfo = MessageBusOptions.HostInfo;
			handlerContext.HandlerLogger = MessageBusOptions.HandlerLogger;
			handlerContext.MessageId = savedMessage.MessageId;
			handlerContext.DisabledMessagePersistence = options.DisabledMessagePersistence;
			handlerContext.ThrowNoHandlerException = true;
			handlerContext.PublisherId = PublisherHelper.GetPublisherIdentifier(MessageBusOptions.HostInfo, traceInfo);
			handlerContext.PublishingTimeUtc = DateTime.UtcNow;
			handlerContext.ParentMessageId = null;
			handlerContext.Timeout = options.Timeout;
			handlerContext.RetryCount = 0;
			handlerContext.ErrorHandling = options.ErrorHandling;
			handlerContext.IdSession = options.IdSession;
			handlerContext.ContentType = options.ContentType;
			handlerContext.ContentEncoding = options.ContentEncoding;
			handlerContext.IsCompressedContent = options.IsCompressContent;
			handlerContext.IsEncryptedContent = options.IsEncryptContent;
			handlerContext.ContainsContent = true;
			handlerContext.Priority = options.Priority;
			handlerContext.Headers = options.Headers?.GetAll();

			handlerContext.Initialize(MessageStatus.Created, null);

			var handlerProcessor = (AsyncVoidMessageHandlerProcessor)_asyncVoidMessageHandlerProcessors.GetOrAdd(
				requestMessageType,
				requestMessageType =>
				{
					var processor = Activator.CreateInstance(typeof(AsyncVoidMessageHandlerProcessor<,>).MakeGenericType(requestMessageType, handlerContext.GetType())) as MessageHandlerProcessorBase;

					if (processor == null)
						result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

					return processor!;
				});

			if (result.HasError())
				return result.Build();

			if (handlerProcessor == null)
				return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

			var handlerResult = await handlerProcessor.HandleAsync(savedMessage.Message, handlerContext, ServiceProvider, cancellationToken);
			result.MergeAllHasError(handlerResult);

			if (isLocalTransactionContext)
			{
				if (result.HasError())
				{
					try
					{
						await transactionContext.TryRollbackAsync(null, cancellationToken);
					}
					catch (Exception rollbackEx)
					{
						var errorMessage = await MessageBusOptions.HostLogger.LogErrorAsync(
							traceInfo,
							MessageBusOptions.HostInfo,
							HostStatus.Unchanged,
							x => x.ExceptionInfo(rollbackEx),
							$"{nameof(SendAsync)}<{message?.GetType().FullName}> rollback error",
							null,
							cancellationToken);

						result.WithError(errorMessage);
					}
				}
				else
				{
					await transactionContext.CommitAsync(cancellationToken);
				}
			}

			return result.WithData(savedMessage.MessageId).Build();
		}
		catch (Exception exHost)
		{
			var errorMessage =
				await MessageBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					MessageBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exHost),
					$"{nameof(SendAsync)}<{message?.GetType().FullName}> error",
					null,
					cancellationToken);

			result.WithError(errorMessage);

			try
			{
				await transactionContext.TryRollbackAsync(exHost, cancellationToken);
			}
			catch (Exception rollbackEx)
			{
				errorMessage = await MessageBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					MessageBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(rollbackEx),
					$"{nameof(SendAsync)}<{message?.GetType().FullName}> rollback error",
					null,
					cancellationToken);

				result.WithError(errorMessage);
			}

			return result.Build();
		}
		finally
		{
			if (isLocalTransactionContext)
			{
				try
				{
					await transactionContext.DisposeAsync();
				}
				catch (Exception disposeEx)
				{
					await MessageBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						MessageBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(disposeEx),
						$"{nameof(SendAsync)}<{message?.GetType().FullName}> dispose error",
						null,
						cancellationToken);
				}
			}
		}
	}

	public Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, optionsBuilder, TraceInfo<Guid>.Create(null, MessageBusOptions.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber), cancellationToken);

	public Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, null, traceInfo, cancellationToken);

	public async Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			var result = new ResultBuilder<ISendResponse<TResponse>, Guid>();
			return result.WithArgumentNullException(traceInfo, nameof(message));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(message.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		return await SendAsync(message, options, isLocalTransactionContext, traceInfo, cancellationToken);
	}

	protected async Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		IMessageOptions options,
		bool isLocalTransactionContext,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<ISendResponse<TResponse>, Guid>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(TraceInfo<Guid>.Create(MessageBusOptions.HostInfo.HostName), nameof(traceInfo));

		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		var transactionContext = options.TransactionContext;
		try
		{
			var requestMessageType = message.GetType();

			var savedMessageResult = await SaveRequestMessageAsync<IRequestMessage<TResponse>, TResponse>(message, options, traceInfo, cancellationToken);
			if (result.MergeHasError(savedMessageResult))
				return result.Build();

			var savedMessage = savedMessageResult.Data;

			if (savedMessage == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedMessage)} == null | {nameof(requestMessageType)} = {requestMessageType.FullName}");
			if (savedMessage.Message == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedMessage)}.{nameof(savedMessage.Message)} == null | {nameof(requestMessageType)} = {requestMessageType.FullName}");

			var handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

			if (handlerContext == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

			handlerContext.TransactionContext = transactionContext;
			handlerContext.ServiceProvider = ServiceProvider;
			handlerContext.TraceInfo = traceInfo;
			handlerContext.HostInfo = MessageBusOptions.HostInfo;
			handlerContext.HandlerLogger = MessageBusOptions.HandlerLogger;
			handlerContext.MessageId = savedMessage.MessageId;
			handlerContext.DisabledMessagePersistence = options.DisabledMessagePersistence;
			handlerContext.ThrowNoHandlerException = true;

			var handlerProcessor = (AsyncMessageHandlerProcessor<TResponse>)_asyncVoidMessageHandlerProcessors.GetOrAdd(
				requestMessageType,
				requestMessageType =>
				{
					var processor = Activator.CreateInstance(typeof(AsyncMessageHandlerProcessor<,,>).MakeGenericType(requestMessageType, typeof(TResponse), handlerContext.GetType())) as MessageHandlerProcessorBase;

					if (processor == null)
						result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

					return processor!;
				});

			if (result.HasError())
				return result.Build();

			if (handlerProcessor == null)
				return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

			var handlerResult = await handlerProcessor.HandleAsync(savedMessage.Message, handlerContext, ServiceProvider, traceInfo, SaveResponseMessageAsync, cancellationToken);
			result.MergeAllWithData(handlerResult);

			if (isLocalTransactionContext)
			{
				if (result.HasError())
				{
					try
					{
						await transactionContext.TryRollbackAsync(null, cancellationToken);
					}
					catch (Exception rollbackEx)
					{
						var errorMessage = await MessageBusOptions.HostLogger.LogErrorAsync(
							traceInfo,
							MessageBusOptions.HostInfo,
							HostStatus.Unchanged,
							x => x.ExceptionInfo(rollbackEx),
							$"{nameof(SendAsync)}<{message?.GetType().FullName}> rollback error",
							null,
							cancellationToken);

						result.WithError(errorMessage);
					}
				}
				else
				{
					await transactionContext.CommitAsync(cancellationToken);
				}
			}

			return result.WithData(handlerResult.Data).Build();
		}
		catch (Exception exHost)
		{
			var errorMessage =
				await MessageBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					MessageBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exHost),
					$"{nameof(SendAsync)}<{message?.GetType().FullName}> error",
					null,
					cancellationToken);

			result.WithError(errorMessage);

			try
			{
				await transactionContext.TryRollbackAsync(exHost, cancellationToken);
			}
			catch (Exception rollbackEx)
			{
				errorMessage = await MessageBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					MessageBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(rollbackEx),
					$"{nameof(SendAsync)}<{message?.GetType().FullName}> rollback error",
					null,
					cancellationToken);

				result.WithError(errorMessage);
			}

			return result.Build();
		}
		finally
		{
			if (isLocalTransactionContext)
			{
				try
				{
					await transactionContext.DisposeAsync();
				}
				catch (Exception disposeEx)
				{
					await MessageBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						MessageBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(disposeEx),
						$"{nameof(SendAsync)}<{message?.GetType().FullName}> dispose error",
						null,
						cancellationToken);
				}
			}
		}
	}

	protected virtual Task<ITransactionContext> CreateTransactionContextAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(ServiceProvider.GetService<ITransactionContextFactory>()?.Create() ?? TransactionContextFactory.CreateTransactionContext());

	protected virtual async Task<IResult<ISavedMessage<TMessage>, Guid>> SaveRequestMessageAsync<TMessage, TResponse>(TMessage requestMessage, IMessageOptions options, ITraceInfo<Guid> traceInfo, CancellationToken cancellation = default)
		where TMessage : class, IRequestMessage<TResponse>
	{
		traceInfo = TraceInfo<Guid>.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TMessage>, Guid>();

		var utcNow = DateTime.UtcNow;
		var metadata = new MessageMetadata<TMessage>
		{
			MessageId = Guid.NewGuid(),
			Message = requestMessage,
			ParentMessageId = null,
			PublishingTimeUtc = utcNow,
			PublisherId = "--MessageBus--",
			TraceInfo = traceInfo,
			Timeout = options.Timeout,
			RetryCount = 0,
			ErrorHandling = options.ErrorHandling,
			IdSession = options.IdSession,
			ContentType = options.ContentType,
			ContentEncoding = options.ContentEncoding,
			IsCompressedContent = options.IsCompressContent,
			IsEncryptedContent = options.IsEncryptContent,
			ContainsContent = requestMessage != null,
			Priority = options.Priority,
			Headers = options.Headers?.GetAll(),
			DisabledMessagePersistence = options.DisabledMessagePersistence,
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null
		};

		if (MessageBusOptions.MessageBodyProvider != null && !options.DisabledMessagePersistence)
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, requestMessage, traceInfo, cancellation);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	protected virtual async Task<IResult<ISavedMessage<TMessage>, Guid>> SaveRequestMessageAsync<TMessage>(TMessage requestMessage, IMessageOptions options, ITraceInfo<Guid> traceInfo, CancellationToken cancellation = default)
		where TMessage : class, IRequestMessage
	{
		traceInfo = TraceInfo<Guid>.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TMessage>, Guid>();

		var utcNow = DateTime.UtcNow;
		var metadata = new MessageMetadata<TMessage>
		{
			MessageId = Guid.NewGuid(),
			Message = requestMessage,
			ParentMessageId = null,
			PublishingTimeUtc = utcNow,
			PublisherId = "--MessageBus--",
			TraceInfo = traceInfo,
			Timeout = options.Timeout,
			RetryCount = 0,
			ErrorHandling = options.ErrorHandling,
			IdSession = options.IdSession,
			ContentType = options.ContentType,
			ContentEncoding = options.ContentEncoding,
			IsCompressedContent = options.IsCompressContent,
			IsEncryptedContent = options.IsEncryptContent,
			ContainsContent = requestMessage != null,
			Priority = options.Priority,
			Headers = options.Headers?.GetAll(),
			DisabledMessagePersistence = options.DisabledMessagePersistence,
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null
		};

		if (MessageBusOptions.MessageBodyProvider != null && !options.DisabledMessagePersistence)
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, requestMessage, traceInfo, cancellation);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	protected virtual async Task<IResult<Guid, Guid>> SaveResponseMessageAsync<TResponse>(TResponse responseMessage, IMessageHandlerContext handlerContext, ITraceInfo<Guid> traceInfo, CancellationToken cancellation = default)
	{
		traceInfo = TraceInfo<Guid>.Create(traceInfo);
		var result = new ResultBuilder<Guid, Guid>();

		if (MessageBusOptions.MessageBodyProvider != null)
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveReplyToStorageAsync(handlerContext.MessageId, responseMessage, traceInfo, cancellation);
			if (result.MergeHasError(saveResult))
				return result.Build();

			result.WithData(saveResult.Data);
		}

		return result.Build();
	}
}
