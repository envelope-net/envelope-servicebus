using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Envelope.Diagnostics;
using Envelope.Services;
using Envelope.Logging;
using Envelope.Logging.Extensions;
using Envelope.Trace;
using Envelope.ServiceBus.Messages;
using Envelope.Extensions;

namespace Envelope.ServiceBus.MessageHandlers.Interceptors;

public abstract class AsyncMessageHandlerInterceptor<TRequestMessage, TResponse, TContext> : IAsyncMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>, IMessageHandlerInterceptor
	where TRequestMessage : IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	protected ILogger Logger { get; }

	public AsyncMessageHandlerInterceptor(ILogger logger)
	{
		Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public virtual async Task<IResult<TResponse>> InterceptHandleAsync(
		TRequestMessage message,
		TContext handlerContext,
		Func<TRequestMessage, TContext, CancellationToken, Task<IResult<TResponse>>> next,
		CancellationToken cancellationToken)
	{
		long callStartTicks = StaticWatch.CurrentTicks;
		long callEndTicks;
		decimal methodCallElapsedMilliseconds = -1;
		Type? messageType = message?.GetType();
		var traceInfo = new TraceInfoBuilder(handlerContext.ServiceProvider!, TraceFrame.Create(), handlerContext.TraceInfo).Build();
		using var scope = Logger.BeginMethodCallScope(traceInfo);

		Logger.LogTraceMessage(
			traceInfo,
			x => x.LogCode(LogCode.Method_In)
					.CommandQueryName(messageType?.FullName),
			true);

		var resultBuilder = new ResultBuilder<TResponse>();
		var result = resultBuilder.Build();
		Guid? idCommand = null;

		try
		{
			//if (handlerOptions.LogMessageEntry)
			//	await LogHandlerEntryAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				var executeResult = await next(message!, handlerContext, cancellationToken).ConfigureAwait(false);
				if (executeResult == null)
					throw new InvalidOperationException($"Interceptor's {nameof(next)} method returns null. Expected {typeof(IResult<TResponse>).FullName}");

				resultBuilder.MergeAllHasError(executeResult);

				if (result.HasError)
				{
					foreach (var errMsg in result.ErrorMessages)
					{
						if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
							errMsg.ClientMessage = handlerContext.ServiceProvider?.GetService<IApplicationContext>()?.ApplicationResources?.GlobalExceptionMessage ?? "Error";

						if (!errMsg.IdCommandQuery.HasValue)
							errMsg.IdCommandQuery = idCommand;

						Logger.LogErrorMessage(errMsg, false);
					}

					if (handlerContext.TransactionController != null)
						handlerContext.TransactionController.ScheduleRollback(result.ToException()!.ToStringTrace());
				}
				else
				{
					resultBuilder.WithData(executeResult.Data);
				}

				callEndTicks = StaticWatch.CurrentTicks;
				methodCallElapsedMilliseconds = StaticWatch.ElapsedMilliseconds(callStartTicks, callEndTicks);
			}
			catch (Exception executeEx)
			{
				callEndTicks = StaticWatch.CurrentTicks;
				methodCallElapsedMilliseconds = StaticWatch.ElapsedMilliseconds(callStartTicks, callEndTicks);

				if (handlerContext.TransactionController != null)
					handlerContext.TransactionController.ScheduleRollback(executeEx.ToStringTrace());

				var clientErrorMessage = handlerContext.ServiceProvider?.GetService<IApplicationContext>()?.ApplicationResources?.GlobalExceptionMessage ?? "Error";

				result = new ResultBuilder<TResponse>()
					.WithError(traceInfo,
						x =>
							x.ExceptionInfo(executeEx)
							.Detail("Unhandled handler exception.")
							.ClientMessage(clientErrorMessage, force: false)
							.IdCommandQuery(idCommand))
					.Build();

				foreach (var errMsg in result.ErrorMessages)
				{
					if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
						errMsg.ClientMessage = clientErrorMessage;

					if (!errMsg.IdCommandQuery.HasValue)
						errMsg.IdCommandQuery = idCommand;

					Logger.LogErrorMessage(errMsg, false);
				}
			}
			//finally
			//{
			//	if (handlerOptions.LogCommandEntry && commandEntryLogger != null && commandEntry != null && startTicks.HasValue)
			//		await LogHandlerExitAsync(cancellationToken).ConfigureAwait(false);
			//}
		}
		catch (Exception interEx)
		{
			callEndTicks = StaticWatch.CurrentTicks;
			methodCallElapsedMilliseconds = StaticWatch.ElapsedMilliseconds(callStartTicks, callEndTicks);

			result = new ResultBuilder<TResponse>()
				.WithError(traceInfo,
					x =>
						x.ExceptionInfo(interEx)
						.Detail($"Unhandled interceptor ({this.GetType().FullName}) exception.")
						.IdCommandQuery(idCommand))
				.Build();

			foreach (var errMsg in result.ErrorMessages)
			{
				if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
					errMsg.ClientMessage = handlerContext.ServiceProvider?.GetService<IApplicationContext>()?.ApplicationResources?.GlobalExceptionMessage ?? "Error";

				if (!errMsg.IdCommandQuery.HasValue)
					errMsg.IdCommandQuery = idCommand;

				Logger.LogErrorMessage(errMsg, false);
			}
		}
		finally
		{
			Logger.LogDebugMessage(
				traceInfo,
				x => x.LogCode(LogCode.Method_Out)
					.CommandQueryName(messageType?.FullName)
					.MethodCallElapsedMilliseconds(methodCallElapsedMilliseconds),
				false);
		}

		return result;
	}

	public virtual Task LogHandlerEntryAsync(CancellationToken cancellationToken)
	{
		//ICommandLogger? commandEntryLogger = null;
		//CommandEntry? commandEntry = null;
		//long? startTicks = null;

		//if (handlerOptions.LogCommandEntry)
		//{
		//	startTicks = StaticWatch.CurrentTicks;
		//	commandEntryLogger = ServiceProvider.GetService<ICommandLogger>();
		//	if (commandEntryLogger == null)
		//		throw new InvalidOperationException($"{nameof(ICommandLogger)} is not configured");

		//	commandEntry = new CommandEntry(typeof(TCommand).ToFriendlyFullName(), traceInfo, handlerOptions.SerializeCommand ? commandBase.Serialize() : null);
		//	commandEntryLogger.WriteCommandEntry(commandEntry);
		//	idCommand = commandEntry?.IdCommandQueryEntry;
		//}

		//commandHandlerContextBuilder.IdCommandEntry(idCommand);

		return Task.CompletedTask;
	}

	public virtual Task LogHandlerExitAsync(CancellationToken cancellationToken)
	{
		//long endTicks = StaticWatch.CurrentTicks;
		//var elapsedMilliseconds = StaticWatch.ElapsedMilliseconds(startTicks.Value, endTicks);
		//commandEntryLogger.WriteCommandExit(commandEntry, elapsedMilliseconds, result.HasError, handlerOptions.SerializeCommand ? commandBase.SerializeResult(result) : null);

		return Task.CompletedTask;
	}
}
