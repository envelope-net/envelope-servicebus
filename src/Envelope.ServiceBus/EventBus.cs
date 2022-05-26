using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Internals;
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

public class EventBus : IEventBus
{
	protected IServiceProvider ServiceProvider { get; }
	protected IEventBusOptions EventBusOptions { get; }
	protected IEventHandlerRegistry EventHandlerRegistry { get; }

	private static readonly ConcurrentDictionary<Type, EventHandlerProcessorBase> _asyncEventHandlerProcessors = new();
	private static readonly ConcurrentDictionary<Type, EventHandlerProcessorBase> _asyncVoidEventHandlerProcessors = new();

	public EventBus(IServiceProvider serviceProvider, IEventBusOptions options, IEventHandlerRegistry eventHandlerRegistry)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		EventBusOptions = options ?? throw new ArgumentNullException(nameof(options));
		EventHandlerRegistry = eventHandlerRegistry ?? throw new ArgumentNullException(nameof(eventHandlerRegistry));
	}

	/// <inheritdoc />
	public Task<IResult<Guid, Guid>> PublishAsync(
		IEvent @event,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(@event, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	/// <inheritdoc />
	public Task<IResult<Guid, Guid>> PublishAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> PublishAsync(@event, optionsBuilder, TraceInfo<Guid>.Create(null, EventBusOptions.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber), cancellationToken);

	/// <inheritdoc />
	public Task<IResult<Guid, Guid>> PublishAsync(
		IEvent @event,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		=> PublishAsync(@event, null, traceInfo, cancellationToken);

	/// <inheritdoc />
	public async Task<IResult<Guid, Guid>> PublishAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		if (@event == null)
		{
			var result = new ResultBuilder<Guid, Guid>();
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		}

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		return await PublishAsync(@event, options, isLocalTransactionContext, traceInfo, cancellationToken);
	}

	protected async Task<IResult<Guid, Guid>> PublishAsync(
		IEvent @event,
		IMessageOptions options,
		bool isLocalTransactionContext,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
	{
		var result = new ResultBuilder<Guid, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(TraceInfo<Guid>.Create(EventBusOptions.HostInfo.HostName), nameof(traceInfo));

		traceInfo = TraceInfo<Guid>.Create(traceInfo);

		var transactionContext = options.TransactionContext;
		try
		{
			var eventType = @event.GetType();

			var savedEventResult = await SaveEventAsync(@event, options, traceInfo, cancellationToken);
			if (result.MergeHasError(savedEventResult))
				return result.Build();

			var savedEvent = savedEventResult.Data;

			if (savedEvent == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)} == null | {nameof(eventType)} = {eventType.FullName}");
			if (savedEvent.Message == null)
				return result.WithInvalidOperationException(traceInfo, $"{nameof(savedEvent)}.{nameof(savedEvent.Message)} == null | {nameof(eventType)} = {eventType.FullName}");

			var handlerContext = EventHandlerRegistry.CreateEventHandlerContext(eventType, ServiceProvider);

			var throwNoHandlerException = options.ThrowNoHandlerException ?? false;

			if (handlerContext == null)
			{
				if (throwNoHandlerException)
				{
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");
				}
				else
				{
					return result.WithWarning(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");
				}
			}

			handlerContext.MessageHandlerResultFactory = EventBusOptions.MessageHandlerResultFactory;
			handlerContext.TransactionContext = transactionContext;
			handlerContext.ServiceProvider = ServiceProvider;
			handlerContext.TraceInfo = traceInfo;
			handlerContext.HostInfo = EventBusOptions.HostInfo;
			handlerContext.HandlerLogger = EventBusOptions.HandlerLogger;
			handlerContext.MessageId = savedEvent.MessageId;
			handlerContext.DisabledMessagePersistence = options.DisabledMessagePersistence;
			handlerContext.ThrowNoHandlerException = throwNoHandlerException;
			handlerContext.PublisherId = PublisherHelper.GetPublisherIdentifier(EventBusOptions.HostInfo, traceInfo);
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

			var handlerProcessor = (AsyncEventHandlerProcessor)_asyncVoidEventHandlerProcessors.GetOrAdd(
				eventType,
				eventType =>
				{
					var processor = Activator.CreateInstance(typeof(AsyncEventHandlerProcessor<,>).MakeGenericType(eventType, handlerContext.GetType())) as EventHandlerProcessorBase;

					if (processor == null)
						result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

					return processor!;
				});

			if (result.HasError())
				return result.Build();

			if (handlerProcessor == null)
				return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

			var handlerResult = await handlerProcessor.HandleAsync(savedEvent.Message, handlerContext, ServiceProvider, cancellationToken);
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
						var errorMessage = await EventBusOptions.HostLogger.LogErrorAsync(
							traceInfo,
							EventBusOptions.HostInfo,
							HostStatus.Unchanged,
							x => x.ExceptionInfo(rollbackEx),
							$"{nameof(PublishAsync)}<{@event?.GetType().FullName}> rollback error",
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

			return result.WithData(savedEvent.MessageId).Build();
		}
		catch (Exception exHost)
		{
			var errorMessage =
				await EventBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					EventBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exHost),
					$"{nameof(PublishAsync)}<{@event?.GetType().FullName}> error",
					null,
					cancellationToken);

			result.WithError(errorMessage);

			try
			{
				await transactionContext.TryRollbackAsync(exHost, cancellationToken);
			}
			catch (Exception rollbackEx)
			{
				errorMessage = await EventBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					EventBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(rollbackEx),
					$"{nameof(PublishAsync)}<{@event?.GetType().FullName}> rollback error",
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
					await EventBusOptions.HostLogger.LogErrorAsync(
						traceInfo,
						EventBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(disposeEx),
						$"{nameof(PublishAsync)}<{@event?.GetType().FullName}> dispose error",
						null,
						cancellationToken);
				}
			}
		}
	}

	protected virtual Task<ITransactionContext> CreateTransactionContextAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(ServiceProvider.GetService<ITransactionContextFactory>()?.Create() ?? TransactionContextFactory.CreateTransactionContext());

	protected virtual async Task<IResult<ISavedMessage<TEvent>, Guid>> SaveEventAsync<TEvent>(TEvent @event, IMessageOptions options, ITraceInfo<Guid> traceInfo, CancellationToken cancellation = default)
		where TEvent : class, IEvent
	{
		traceInfo = TraceInfo<Guid>.Create(traceInfo);
		var result = new ResultBuilder<ISavedMessage<TEvent>, Guid>();

		var utcNow = DateTime.UtcNow;
		var metadata = new MessageMetadata<TEvent>
		{
			MessageId = Guid.NewGuid(),
			Message = @event,
			ParentMessageId = null,
			PublishingTimeUtc = utcNow,
			PublisherId = "--EventBus--",
			TraceInfo = traceInfo,
			Timeout = options.Timeout,
			RetryCount = 0,
			ErrorHandling = options.ErrorHandling,
			IdSession = options.IdSession,
			ContentType = options.ContentType,
			ContentEncoding = options.ContentEncoding,
			IsCompressedContent = options.IsCompressContent,
			IsEncryptedContent = options.IsEncryptContent,
			ContainsContent = @event != null,
			Priority = options.Priority,
			Headers = options.Headers?.GetAll(),
			DisabledMessagePersistence = options.DisabledMessagePersistence,
			MessageStatus = MessageStatus.Created,
			DelayedToUtc = null
		};

		if (EventBusOptions.EventBodyProvider != null && !options.DisabledMessagePersistence)
		{
			var saveResult = await EventBusOptions.EventBodyProvider.SaveToStorageAsync(new List<IMessageMetadata> { metadata }, @event, traceInfo, cancellation);
			if (result.MergeHasError(saveResult))
				return result.Build();
		}

		return result.WithData(metadata).Build();
	}

	#region IEventPublisher

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo<Guid>.Create(null, EventBusOptions.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		var publishResult = await PublishAsync(@event, options, isLocalTransactionContext, traceInfo, cancellationToken);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo<Guid>.Create(null, EventBusOptions.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		var publishResult = await PublishAsync(@event, options, isLocalTransactionContext, traceInfo, cancellationToken);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		var publishResult = await PublishAsync(@event, options, isLocalTransactionContext, traceInfo, cancellationToken);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		var isLocalTransactionContext = false;
		if (options.TransactionContext == null)
		{
			options.TransactionContext = await CreateTransactionContextAsync(cancellationToken).ConfigureAwait(false);
			isLocalTransactionContext = true;
		}

		var publishResult = await PublishAsync(@event, options, isLocalTransactionContext, traceInfo, cancellationToken);
		if (result.MergeAllHasError(publishResult))
			return result.Build();

		return result.WithData(new List<Guid> { publishResult.Data }).Build();
	}

	#endregion IEventPublisher
}
