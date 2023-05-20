using Envelope.Logging;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.Trace;
using Envelope.Transactions;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus.MessageHandlers;

public abstract class MessageHandlerContext : IMessageHandlerContext
{
	public IServiceProvider ServiceProvider { get; private set; }

	public IHostInfo HostInfo { get; private set; }

	public ITransactionController TransactionController { get; private set; }

	public ITraceInfo TraceInfo { get; private set; }

	public IHandlerLogger HandlerLogger { get; private set; }

	private bool _initialized;
	private readonly object _initLock = new();
	public void Initialize(
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		IHostInfo hostInfo,
		ITransactionController transactionController,
		IHandlerLogger handlerLogger)
	{
		if (_initialized)
			throw new InvalidOperationException("Already initialized");

		lock (_initLock)
		{
			if (_initialized)
				throw new InvalidOperationException("Already initialized");

			ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			TraceInfo = traceInfo ?? throw new ArgumentNullException(nameof(traceInfo));
			HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
			TransactionController = transactionController ?? throw new ArgumentNullException(nameof(transactionController));
			HandlerLogger = handlerLogger ?? throw new ArgumentNullException(nameof(handlerLogger));

			_initialized = true;
		}
	}

	public ITraceInfo CreateTraceInfo(
		IEnumerable<MethodParameter>? methodParameters = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		var traceInfo =
			new TraceInfoBuilder(
				ServiceProvider,
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
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogCritical(traceInfo, messageBuilder, detail, transactionCoordinator);

	public virtual Task<IErrorMessage?> LogCriticalAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null) //TODO EXPRESS
			return Task.FromResult((IErrorMessage?)null);
		else
			return HandlerLogger.LogCriticalAsync(traceInfo, messageBuilder, detail, transactionCoordinator, cancellationToken)!;
	}

	public virtual ILogMessage? LogDebug(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogDebug(traceInfo, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogDebugAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null //TODO EXPRESS
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogDebugAsync(traceInfo, messageBuilder, detail, transactionCoordinator, cancellationToken);

	public virtual IErrorMessage? LogError(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogError(traceInfo, messageBuilder, detail, transactionCoordinator);

	public virtual Task<IErrorMessage?> LogErrorAsync(
		ITraceInfo traceInfo,
		Action<ErrorMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
	{
		if (HandlerLogger == null) //TODO EXPRESS
			return Task.FromResult((IErrorMessage?)null);
		else
			return HandlerLogger.LogErrorAsync(traceInfo, messageBuilder, detail, transactionCoordinator, cancellationToken)!;
	}

	public virtual ILogMessage? LogInformation(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogInformation(traceInfo, messageBuilder, detail, force, transactionCoordinator);

	public virtual Task<ILogMessage?> LogInformationAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null //TODO EXPRESS
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogInformationAsync(traceInfo, messageBuilder, detail, force, transactionCoordinator, cancellationToken);

	public virtual ILogMessage? LogTrace(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogTrace(traceInfo, messageBuilder, detail, transactionCoordinator);

	public virtual Task<ILogMessage?> LogTraceAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null //TODO EXPRESS
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogTraceAsync(traceInfo, messageBuilder, detail, transactionCoordinator, cancellationToken);

	public virtual ILogMessage? LogWarning(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null)
		=> HandlerLogger.LogWarning(traceInfo, messageBuilder, detail, force, transactionCoordinator);

	public virtual Task<ILogMessage?> LogWarningAsync(
		ITraceInfo traceInfo,
		Action<LogMessageBuilder> messageBuilder,
		string? detail = null,
		bool force = false,
		ITransactionCoordinator? transactionCoordinator = null,
		CancellationToken cancellationToken = default)
		=> HandlerLogger == null //TODO EXPRESS
			? Task.FromResult((ILogMessage?)null)
			: HandlerLogger.LogWarningAsync(traceInfo, messageBuilder, detail, force, transactionCoordinator, cancellationToken);
}

