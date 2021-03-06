using Envelope.Exceptions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Configuration.Internal;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public partial class MessageBus : IMessageBus
{
	protected IServiceProvider ServiceProvider { get; }
	protected IMessageBusOptions MessageBusOptions { get; }
	protected IMessageHandlerRegistry MessageHandlerRegistry { get; }

	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncMessageHandlerProcessors = new();
	private static readonly ConcurrentDictionary<Type, MessageHandlerProcessorBase> _asyncVoidMessageHandlerProcessors = new();

	public MessageBus(IServiceProvider serviceProvider, IMessageBusConfiguration configuration, IMessageHandlerRegistry messageHandlerRegistry)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		MessageHandlerRegistry = messageHandlerRegistry ?? throw new ArgumentNullException(nameof(messageHandlerRegistry));

		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		var error = configuration.Validate(nameof(MessageBusConfiguration));
		if (0 < error?.Count)
			throw new ConfigurationException(error);

		MessageBusOptions = new MessageBusOptions()
		{
			HostInfo = new HostInfo(configuration.MessageBusName),
			HostLogger = configuration.HostLogger(serviceProvider),
			HandlerLogger = configuration.HandlerLogger(serviceProvider),
			MessageHandlerResultFactory = configuration.MessageHandlerResultFactory(serviceProvider),
			MessageBodyProvider = configuration.MessageBodyProvider
		};

		error = MessageBusOptions.Validate(nameof(MessageBusOptions));
		if (0 < error?.Count)
			throw new ConfigurationException(error);
	}

	public Task<IResult<Guid>> SendAsync(
		IRequestMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<Guid>> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(
			message,
			optionsBuilder,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //MessageBusOptions.HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	public Task<IResult<Guid>> SendAsync(
		IRequestMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, (Action<MessageOptionsBuilder>?)null, traceInfo, cancellationToken);

	public async Task<IResult<Guid>> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			var result = new ResultBuilder<Guid>();
			return result.WithArgumentNullException(traceInfo, nameof(message));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(message.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionManager = false;
		if (options.TransactionContext == null)
		{
			var transactionManager = await CreateTransactionManagerAsync(cancellationToken).ConfigureAwait(false);
			options.TransactionContext = transactionManager.CreateTransactionContext();
			isLocalTransactionManager = true;
		}

		return await SendAsync(message, options, isLocalTransactionManager, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	protected async Task<IResult<Guid>> SendAsync(
		IRequestMessage message,
		IMessageOptions options,
		bool isLocalTransactionManager,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<Guid>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//MessageBusOptions.HostInfo.HostName
					),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		var transactionContext = options.TransactionContext;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var requestMessageType = message.GetType();

				var savedMessageResult = await SaveRequestMessageAsync(message, options, traceInfo, cancellationToken).ConfigureAwait(false);
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

				var handlerResult = await handlerProcessor.HandleAsync(savedMessage.Message, handlerContext, ServiceProvider, cancellationToken).ConfigureAwait(false);
				result.MergeAllHasError(handlerResult);

				if (result.HasError())
				{
					transactionContext.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionManager)
						transactionContext.ScheduleCommit();
				}

				return result.WithData(savedMessage.MessageId).Build();
			},
			$"{nameof(SendAsync)}<{message?.GetType().FullName}> return {typeof(IResult<Guid>).FullName}",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await MessageBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						MessageBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				return errorMessage;
			},
			null,
			isLocalTransactionManager,
			cancellationToken).ConfigureAwait(false);
	}

	public Task<IResult<ISendResponse<TResponse>>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<ISendResponse<TResponse>>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> SendAsync(
			message,
			optionsBuilder,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //MessageBusOptions.HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	public Task<IResult<ISendResponse<TResponse>>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		=> SendAsync(message, null, traceInfo, cancellationToken);

	public async Task<IResult<ISendResponse<TResponse>>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			var result = new ResultBuilder<ISendResponse<TResponse>>();
			return result.WithArgumentNullException(traceInfo, nameof(message));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(message.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionManager = false;
		if (options.TransactionContext == null)
		{
			var transactionManager = await CreateTransactionManagerAsync(cancellationToken).ConfigureAwait(false);
			options.TransactionContext = transactionManager.CreateTransactionContext();
			isLocalTransactionManager = true;
		}

		return await SendAsync(message, options, isLocalTransactionManager, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	protected async Task<IResult<ISendResponse<TResponse>>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		IMessageOptions options,
		bool isLocalTransactionManager,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<ISendResponse<TResponse>>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//MessageBusOptions.HostInfo.HostName
					),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		var transactionContext = options.TransactionContext;

		return await ServiceTransactionInterceptor.ExecuteActionAsync(
			false,
			traceInfo,
			transactionContext,
			async (traceInfo, transactionContext, cancellationToken) =>
			{
				var requestMessageType = message.GetType();

				var savedMessageResult = await SaveRequestMessageAsync<IRequestMessage<TResponse>, TResponse>(message, options, traceInfo, cancellationToken).ConfigureAwait(false);
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

				var handlerResult = await handlerProcessor.HandleAsync(savedMessage.Message, handlerContext, ServiceProvider, traceInfo, SaveResponseMessageAsync, cancellationToken).ConfigureAwait(false);
				result.MergeAllWithData(handlerResult);

				if (result.HasError())
				{
					transactionContext.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionManager)
						transactionContext.ScheduleCommit();
				}

				return result.WithData(handlerResult.Data).Build();
			},
			$"{nameof(SendAsync)}<{message?.GetType().FullName}> return {typeof(IResult<ISendResponse<TResponse>>).FullName}",
			async (traceInfo, exception, detail) =>
			{
				var errorMessage =
					await MessageBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						MessageBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null,
						cancellationToken: default).ConfigureAwait(false);

				return errorMessage;
			},
			null,
			isLocalTransactionManager,
			cancellationToken).ConfigureAwait(false);
	}

	protected virtual Task<ITransactionManager> CreateTransactionManagerAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(ServiceProvider.GetService<ITransactionManagerFactory>()?.Create() ?? TransactionManagerFactory.CreateTransactionManager());

	protected virtual async Task<IResult<ISavedMessage<TMessage>>> SaveRequestMessageAsync<TMessage, TResponse>(
		TMessage requestMessage,
		IMessageOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IRequestMessage<TResponse>
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TMessage>>();

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

		if (MessageBusOptions.MessageBodyProvider != null
			&& MessageBusOptions.MessageBodyProvider.AllowMessagePersistence(options.DisabledMessagePersistence, metadata))
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, requestMessage, traceInfo, options.TransactionContext, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	protected virtual async Task<IResult<ISavedMessage<TMessage>>> SaveRequestMessageAsync<TMessage>(
		TMessage requestMessage,
		IMessageOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IRequestMessage
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TMessage>>();

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

		if (MessageBusOptions.MessageBodyProvider != null
			&& MessageBusOptions.MessageBodyProvider.AllowMessagePersistence(options.DisabledMessagePersistence, metadata))
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, requestMessage, traceInfo, options.TransactionContext, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	protected virtual async Task<IResult<Guid>> SaveResponseMessageAsync<TResponse>(
		TResponse responseMessage,
		IMessageHandlerContext handlerContext,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<Guid>();

		if (MessageBusOptions.MessageBodyProvider != null)
		{
			var saveResult = await MessageBusOptions.MessageBodyProvider.SaveReplyToStorageAsync(handlerContext.MessageId, responseMessage, traceInfo, handlerContext.TransactionContext, cancellationToken).ConfigureAwait(false);
			if (result.MergeHasError(saveResult))
				return result.Build();

			result.WithData(saveResult.Data);
		}

		return result.Build();
	}
}
