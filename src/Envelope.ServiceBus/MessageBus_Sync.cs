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

public partial class MessageBus : IMessageBus
{
	public IResult Send(
		IRequestMessage message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Send(message, null!, memberName, sourceFilePath, sourceLineNumber);

	public IResult Send(
		IRequestMessage message,
		ITransactionController transactionController,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Send(
			message,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber));

	public IResult Send(
		IRequestMessage message,
		ITraceInfo traceInfo)
		=> Send(message, null!, traceInfo);

	public IResult Send(
		IRequestMessage message,
		ITransactionController transactionController,
		ITraceInfo traceInfo)
	{
		if (message == null)
		{
			var result = new ResultBuilder();
			return result.WithArgumentNullException(traceInfo, nameof(message));
		}

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		return SendInternal(message, transactionController, isLocalTransactionCoordinator, traceInfo);
	}

	protected IResult SendInternal(
		IRequestMessage message,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo)
	{
		var result = new ResultBuilder();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
					HostInfo.HostName),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		VoidMessageHandlerProcessor? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return ServiceTransactionInterceptor.ExecuteAction(
			false,
			traceInfo,
			transactionController,
			(traceInfo, transactionController, unhandledExceptionDetail) =>
			{
				var requestMessageType = message.GetType();

				handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (VoidMessageHandlerProcessor)_asyncVoidMessageHandlerProcessors.GetOrAdd(
					requestMessageType,
					requestMessageType =>
					{
						var processor = Activator.CreateInstance(typeof(VoidMessageHandlerProcessor<,>).MakeGenericType(requestMessageType, handlerContext.GetType())) as MessageHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

				var handlerResult = handlerProcessor.Handle(message, handlerContext, ServiceProvider, unhandledExceptionDetail);
				result.MergeAllHasError(handlerResult);

				if (result.HasError())
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
			$"{nameof(Send)}<{message?.GetType().FullName}> return {typeof(IResult).FullName}",
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
						handlerProcessor.OnError(traceInfo, exception, null, detail, message, handlerContext, ServiceProvider);
					}
					catch { }
				}

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator);
	}

	public IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Send(message, null!, memberName, sourceFilePath, sourceLineNumber);

	public IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		=> Send(
			message,
			transactionController,
			TraceInfo.Create(
				ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber));

	public IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo)
		=> Send(message, null!, traceInfo);

	public IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<TResponse>();
		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));

		var isLocalTransactionCoordinator = false;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		var sendResult = SendInternal(message, transactionController, isLocalTransactionCoordinator, traceInfo);
		result.MergeAllHasError(sendResult);

		if (sendResult.Data != null)
			result.WithData(sendResult.Data);

		return result.Build();
	}

	protected IResult<TResponse> SendInternal<TResponse>(
		IRequestMessage<TResponse> message,
		ITransactionController transactionController,
		bool isLocalTransactionCoordinator,
		ITraceInfo traceInfo)
	{
		var result = new ResultBuilder<TResponse>();

		if (message == null)
			return result.WithArgumentNullException(traceInfo, nameof(message));
		if (transactionController == null)
			return result.WithArgumentNullException(traceInfo, nameof(transactionController));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
					HostInfo.HostName),
				nameof(traceInfo));

		traceInfo = TraceInfo.Create(traceInfo);

		MessageHandlerProcessor<TResponse>? handlerProcessor = null;
		IMessageHandlerContext? handlerContext = null;

		return ServiceTransactionInterceptor.ExecuteAction(
			false,
			traceInfo,
			transactionController,
			(traceInfo, transactionController, unhandledExceptionDetail) =>
			{
				var requestMessageType = message.GetType();

				handlerContext = MessageHandlerRegistry.CreateMessageHandlerContext(requestMessageType, ServiceProvider);

				if (handlerContext == null)
					return result.WithInvalidOperationException(traceInfo, $"{nameof(handlerContext)} == null| {nameof(requestMessageType)} = {requestMessageType.FullName}");

				handlerContext.Initialize(
					ServiceProvider,
					traceInfo,
					HostInfo,
					transactionController,
					HandlerLogger);

				handlerProcessor = (MessageHandlerProcessor<TResponse>)_asyncVoidMessageHandlerProcessors.GetOrAdd(
					requestMessageType,
					requestMessageType =>
					{
						var processor = Activator.CreateInstance(typeof(MessageHandlerProcessor<,,>).MakeGenericType(requestMessageType, typeof(TResponse), handlerContext.GetType())) as MessageHandlerProcessorBase;

						if (processor == null)
							result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

						return processor!;
					});

				if (result.HasError())
					return result.Build();

				if (handlerProcessor == null)
					return result.WithInvalidOperationException(traceInfo, $"Could not create handlerProcessor type for {requestMessageType}");

				var handlerResult = handlerProcessor.Handle(message, handlerContext, ServiceProvider, traceInfo, unhandledExceptionDetail);
				result.MergeAllWithData(handlerResult);

				if (result.HasError())
				{
					transactionController.ScheduleRollback();
				}
				else
				{
					if (isLocalTransactionCoordinator)
						transactionController.ScheduleCommit();
				}

				return result.WithData(handlerResult.Data).Build();
			},
			$"{nameof(Send)}<{message?.GetType().FullName}> return {typeof(IResult<TResponse>).FullName}",
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
						handlerProcessor.OnError(traceInfo, exception, null, detail, message, handlerContext, ServiceProvider);
					}
					catch { }
				}

				return errorMessage;
			},
			null,
			isLocalTransactionCoordinator);
	}
}
