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

public abstract class EventHandlerInterceptor<TEvent, TContext> : IEventHandlerInterceptor<TEvent, TContext>, IEventHandlerInterceptor
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	protected ILogger Logger { get; }

	public EventHandlerInterceptor(ILogger logger)
	{
		Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public virtual IResult InterceptHandle(
		TEvent @event,
		TContext handlerContext,
		Func<TEvent, TContext, IResult> next)
	{
		long callStartTicks = StaticWatch.CurrentTicks;
		long callEndTicks;
		decimal methodCallElapsedMilliseconds = -1;
		Type? eventType = @event?.GetType();
		var traceInfo = new TraceInfoBuilder(handlerContext.ServiceProvider!, TraceFrame.Create(), handlerContext.TraceInfo).Build();
		using var scope = Logger.BeginMethodCallScope(traceInfo);

		Logger.LogTraceMessage(
			traceInfo,
			x => x.LogCode(LogCode.Method_In)
					.CommandQueryName(eventType?.FullName),
			true);

		var resultBuilder = new ResultBuilder();
		var result = resultBuilder.Build();
		Guid? idEvent = null;

		try
		{
			//if (handlerOptions.LogEventEntry)
			//	LogHandlerEntry();

			try
			{
				var executeResult = next(@event!, handlerContext);
				if (executeResult == null)
					throw new InvalidOperationException($"Interceptor's {nameof(next)} method returns null. Expected {typeof(IResult).FullName}");

				resultBuilder.MergeAllHasError(executeResult);

				if (result.HasError)
				{
					foreach (var errMsg in result.ErrorMessages)
					{
						if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
							errMsg.ClientMessage = handlerContext.ServiceProvider?.GetService<IApplicationContext>()?.ApplicationResources?.GlobalExceptionMessage ?? "Error";

						if (!errMsg.IdCommandQuery.HasValue)
							errMsg.IdCommandQuery = idEvent;

						Logger.LogErrorMessage(errMsg, false);
					}

					if (handlerContext.TransactionController != null)
						handlerContext.TransactionController.ScheduleRollback(result.ToException()!.ToStringTrace());
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

				result = new ResultBuilder()
					.WithError(traceInfo,
						x =>
							x.ExceptionInfo(executeEx)
							.Detail("Unhandled handler exception.")
							.ClientMessage(clientErrorMessage, force: false)
							.IdCommandQuery(idEvent))
					.Build();

				foreach (var errMsg in result.ErrorMessages)
				{
					if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
						errMsg.ClientMessage = clientErrorMessage;

					if (!errMsg.IdCommandQuery.HasValue)
						errMsg.IdCommandQuery = idEvent;

					Logger.LogErrorMessage(errMsg, false);
				}
			}
			//finally
			//{
			//	if (handlerOptions.LogEventEntry && commandEntryLogger != null && commandEntry != null && startTicks.HasValue)
			//		LogHandlerExit();
			//}
		}
		catch (Exception interEx)
		{
			callEndTicks = StaticWatch.CurrentTicks;
			methodCallElapsedMilliseconds = StaticWatch.ElapsedMilliseconds(callStartTicks, callEndTicks);

			result = new ResultBuilder()
				.WithError(traceInfo,
					x =>
						x.ExceptionInfo(interEx)
						.Detail($"Unhandled interceptor ({this.GetType().FullName}) exception.")
						.IdCommandQuery(idEvent))
				.Build();

			foreach (var errMsg in result.ErrorMessages)
			{
				if (string.IsNullOrWhiteSpace(errMsg.ClientMessage))
					errMsg.ClientMessage = handlerContext.ServiceProvider?.GetService<IApplicationContext>()?.ApplicationResources?.GlobalExceptionMessage ?? "Error";

				if (!errMsg.IdCommandQuery.HasValue)
					errMsg.IdCommandQuery = idEvent;

				Logger.LogErrorMessage(errMsg, false);
			}
		}
		finally
		{
			Logger.LogDebugMessage(
				traceInfo,
				x => x.LogCode(LogCode.Method_Out)
					.CommandQueryName(eventType?.FullName)
					.MethodCallElapsedMilliseconds(methodCallElapsedMilliseconds),
				true);
		}

		return result;
	}

	public virtual void LogHandlerEntry()
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
	}

	public virtual void LogHandlerExit()
	{
		//long endTicks = StaticWatch.CurrentTicks;
		//var elapsedMilliseconds = StaticWatch.ElapsedMilliseconds(startTicks.Value, endTicks);
		//commandEntryLogger.WriteCommandExit(commandEntry, elapsedMilliseconds, result.HasError, handlerOptions.SerializeCommand ? commandBase.SerializeResult(result) : null);
	}
}
