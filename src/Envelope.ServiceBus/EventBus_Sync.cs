using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Processors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public partial class EventBus : IEventBus
{
	/// <inheritdoc />
	public IResult Publish(
		IEvent @event,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Publish(@event, null!, memberName, sourceFilePath, sourceLineNumber);

	/// <inheritdoc />
	public IResult Publish(
		IEvent @event,
		ITransactionController transactionController,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Publish(
			@event,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber));

	/// <inheritdoc />
	public IResult Publish(
		IEvent @event,
		ITraceInfo traceInfo)
		=> Publish(@event, null!, traceInfo);

	/// <inheritdoc />
	public IResult Publish(
		IEvent @event,
		ITransactionController transactionController,
		ITraceInfo traceInfo)
	{
		if (@event == null)
		{
			var result = new ResultBuilder();
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		}

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		return PublishInternal(@event, transactionController, isLocalTransactionCoordinator, traceInfo);
	}

	protected IResult PublishInternal(
		IEvent @event,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo)
	{
		var result = new ResultBuilder();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//HostInfo.HostName
					),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		EventHandlerProcessor? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return ServiceTransactionInterceptor.ExecuteAction(
			false,
			traceInfo,
			transactionController,
			(traceInfo, transactionController, unhandledExceptionDetail) =>
			{
				var eventType = @event.GetType();

				handlerContext = EventHandlerRegistry.CreateEventHandlerContext(eventType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(eventType)} = {eventType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (EventHandlerProcessor)_asyncVoidEventHandlerProcessors.GetOrAdd(
					eventType,
					eventType =>
					{
						var processor = Activator.CreateInstance(typeof(EventHandlerProcessor<,>).MakeGenericType(eventType, handlerContext.GetType())) as EventHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {eventType}");

				var handlerResult = handlerProcessor.Handle(@event, handlerContext, ServiceProvider, unhandledExceptionDetail);
				result.MergeAll(handlerResult);

				if (result.HasTransactionRollbackError())
				{
					transactionController.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionCoordinator)
						transactionController.ScheduleCommit();
				}

				return result.Build();
			},
			$"{nameof(Publish)}<{@event?.GetType().FullName}>",
			(traceInfo, exception, detail) =>
			{
				var errorMessage =
					HostLogger.LogError(
						traceInfo,
						HostInfo,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						null);

				if (handlerProcessor != null)
				{
					try
					{
						handlerProcessor.OnError(traceInfo, exception, null, detail, @event, handlerContext, ServiceProvider);
					}
					catch { }
				}

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator);
	}
}
