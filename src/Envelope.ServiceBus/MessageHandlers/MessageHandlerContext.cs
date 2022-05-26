using Envelope.Logging;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;

namespace Envelope.ServiceBus.MessageHandlers;

public abstract class MessageHandlerContext : IMessageHandlerContext, IMessageMetadata, IMessageInfo
{
	public bool ThrowNoHandlerException { get; internal set; }

	public ITransactionContext? TransactionContext { get; internal set; }

	public IServiceProvider? ServiceProvider { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public IMessageHandlerResultFactory MessageHandlerResultFactory { get; internal set; }

	public ITraceInfo<Guid> TraceInfo { get; internal set; }

	public IHostInfo HostInfo { get; internal set; }

	public string PublisherId { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	internal IHandlerLogger? HandlerLogger { get; set; }

	public Guid MessageId { get; internal set; }

	public Guid? ParentMessageId { get; internal set; }

	public bool Processed { get => false; set => throw new NotSupportedException($"Set {nameof(Processed)} is not supported in {nameof(MessageHandlerContext)}"); }

	public bool DisabledMessagePersistence { get; internal set; }

	public DateTime PublishingTimeUtc { get; internal set; }

	public TimeSpan? Timeout { get; internal set; }

	public DateTime? TimeToLiveUtc => Timeout.HasValue
		? PublishingTimeUtc.Add(Timeout.Value)
		: null;

	public Guid? IdSession { get; internal set; }

	public string? ContentType { get; internal set; }

	public Encoding? ContentEncoding { get; internal set; }

	public bool IsCompressedContent { get; internal set; }

	public bool IsEncryptedContent { get; internal set; }

	public bool ContainsContent { get; internal set; }

	public int Priority { get; internal set; }

	public IEnumerable<KeyValuePair<string, object>>? Headers { get; internal set; }

	public MessageStatus MessageStatus { get; private set; }

	public int RetryCount { get; internal set; }

	public IErrorHandlingController? ErrorHandling { get; internal set; }

	public DateTime? DelayedToUtc { get; private set; }

	private bool initialized;
	internal void Initialize(MessageStatus messageStatus, DateTime? delayedToUtc)
	{
		if (initialized)
			throw new InvalidOperationException($"{nameof(MessageHandlerContext)} already initialized");

		initialized = true;
		MessageStatus = messageStatus;
		DelayedToUtc = delayedToUtc;
	}

	public MethodLogScope<Guid> CreateScope(
		ILogger logger,
		IEnumerable<MethodParameter>? methodParameters = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		if (logger == null)
			throw new ArgumentNullException(nameof(logger));

		var traceInfo =
			new TraceInfoBuilder<Guid>(
				HostInfo.HostName,
				new TraceFrameBuilder(TraceInfo?.TraceFrame)
					.CallerMemberName(memberName)
					.CallerFilePath(sourceFilePath)
					.CallerLineNumber(sourceLineNumber == 0 ? (int?)null : sourceLineNumber)
					.MethodParameters(methodParameters)
					.Build(),
				TraceInfo)
				.Build();

		var disposable = logger.BeginScope(new Dictionary<string, Guid?>
		{
			[nameof(ILogMessage.TraceInfo.TraceFrame.MethodCallId)] = traceInfo.TraceFrame.MethodCallId,
			[nameof(ILogMessage.TraceInfo.CorrelationId)] = traceInfo.CorrelationId
		});

		var scope = new MethodLogScope<Guid>(traceInfo, disposable);
		return scope;
	}

	public virtual IErrorMessage<Guid>? LogCritical(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogCritical(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<IErrorMessage<Guid>?> LogCriticalAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null)
			return Task.FromResult((IErrorMessage<Guid>?)null);
		else
			return HandlerLogger.LogCriticalAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken)!;
	}

	public virtual ILogMessage<Guid>? LogDebug(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogDebug(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<ILogMessage<Guid>?> LogDebugAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage<Guid>?)null)
			: HandlerLogger.LogDebugAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken);

	public virtual IErrorMessage<Guid>? LogError(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogError(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<IErrorMessage<Guid>?> LogErrorAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null)
			return Task.FromResult((IErrorMessage<Guid>?)null);
		else
			return HandlerLogger.LogErrorAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken)!;
	}

	public virtual ILogMessage<Guid>? LogInformation(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogInformation(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<ILogMessage<Guid>?> LogInformationAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage<Guid>?)null)
			: HandlerLogger.LogInformationAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken);

	public virtual ILogMessage<Guid>? LogTrace(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogTrace(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<ILogMessage<Guid>?> LogTraceAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage<Guid>?)null)
			: HandlerLogger.LogTraceAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken);

	public virtual ILogMessage<Guid>? LogWarning(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null)
		=> HandlerLogger?.LogWarning(traceInfo, messageMetadata, messageBuilder, detail, transactionContext);

	public virtual Task<ILogMessage<Guid>?> LogWarningAsync(
		ITraceInfo<Guid> traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder<Guid>> messageBuilder,
		string? detail = null,
		ITransactionContext? transactionContext = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage<Guid>?)null)
			: HandlerLogger.LogWarningAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionContext, cancellationToken);
}

