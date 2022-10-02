using Envelope.Logging;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using Envelope.Transactions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Envelope.ServiceBus.MessageHandlers;

public abstract class MessageHandlerContext : IMessageHandlerContext, IMessageMetadata, IMessageInfo
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public IServiceBusOptions? ServiceBusOptions { get; internal set; }

	public bool ThrowNoHandlerException { get; internal set; }

	public ITransactionController TransactionController { get; internal set; }

	public IServiceProvider? ServiceProvider { get; internal set; }
	public IMessageHandlerResultFactory MessageHandlerResultFactory { get; internal set; }

	public ITraceInfo TraceInfo { get; internal set; }

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

	public bool HasSelfContent { get; set; }

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

	internal void Update(bool processed, MessageStatus status, int retryCount, DateTime? delayedToUtc)
	{
		throw new NotImplementedException();

		//Processed = processed;
		//MessageStatus = status;
		//RetryCount = retryCount;
		//DelayedToUtc = delayedToUtc;
	}

	void IMessageMetadata.UpdateInternal(bool processed, MessageStatus status, int retryCount, DateTime? delayedToUtc)
		=> Update(processed, status, retryCount, delayedToUtc);

	//public MethodLogScope CreateScope(
	//	ILogger logger,
	//	IEnumerable<MethodParameter>? methodParameters = null,
	//	[CallerMemberName] string memberName = "",
	//	[CallerFilePath] string sourceFilePath = "",
	//	[CallerLineNumber] int sourceLineNumber = 0)
	//{
	//	if (logger == null)
	//		throw new ArgumentNullException(nameof(logger));

	//	var traceInfo =
	//		new TraceInfoBuilder(
	//			(ServiceProvider ?? ServiceBusOptions?.ServiceProvider)!,
	//			new TraceFrameBuilder(TraceInfo?.TraceFrame)
	//				.CallerMemberName(memberName)
	//				.CallerFilePath(sourceFilePath)
	//				.CallerLineNumber(sourceLineNumber == 0 ? (int?)null : sourceLineNumber)
	//				.MethodParameters(methodParameters)
	//				.Build(),
	//			TraceInfo)
	//			.Build();

	//	var disposable = logger.BeginScope(new Dictionary<string, Guid?>
	//	{
	//		[nameof(ILogMessage.TraceInfo.TraceFrame.MethodCallId)] = traceInfo.TraceFrame.MethodCallId,
	//		[nameof(ILogMessage.TraceInfo.CorrelationId)] = traceInfo.CorrelationId
	//	});

	//	var scope = new MethodLogScope(traceInfo, disposable);
	//	return scope;
	//}

	public ITraceInfo CreateTraceInfo(
		IEnumerable<MethodParameter>? methodParameters = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		var traceInfo =
			new TraceInfoBuilder(
				(ServiceProvider ?? ServiceBusOptions?.ServiceProvider)!,
				new TraceFrameBuilder(TraceInfo?.TraceFrame)
					.CallerMemberName(memberName)
					.CallerFilePath(sourceFilePath)
					.CallerLineNumber(sourceLineNumber == 0 ? (int?)null : sourceLineNumber)
					.MethodParameters(methodParameters)
					.Build(),
				TraceInfo)
				.Build();

		return traceInfo;
	}

	public virtual IErrorMessage? LogCritical(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogCritical(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<IErrorMessage?> LogCriticalAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null)
			return Task.FromResult((IErrorMessage?)null);
		else
			return HandlerLogger.LogCriticalAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken)!;
	}

	public virtual ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogDebug(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogDebugAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken);

	public virtual IErrorMessage? LogError(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogError(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<IErrorMessage?> LogErrorAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null)
			return Task.FromResult((IErrorMessage?)null);
		else
			return HandlerLogger.LogErrorAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken)!;
	}

	public virtual ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogInformation(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogInformationAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken);

	public virtual ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogTrace(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogTraceAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken);

	public virtual ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger?.LogWarning(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		IMessageMetadata? messageMetadata,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogWarningAsync(traceInfo, messageMetadata, messageBuilder, detail, transactionCoordinator, cancellationToken);
}

