using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class MessageHandlerProcessor<TResponse> : MessageHandlerProcessorBase
{
	public abstract IResult<ISendResponse<TResponse>, Guid> Handle(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo<Guid> traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo<Guid>, IResult<Guid, Guid>> saveResponseMessageAction);
}

internal class MessageHandlerProcessor<TRequestMessage, TResponse, TContext> : MessageHandlerProcessor<TResponse>
	where TRequestMessage : Messages.IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	protected override IMessageHandler CreateHandler(IServiceProvider serviceProvider)
	{
		var handler = serviceProvider.GetService<IMessageHandler<TRequestMessage, TResponse, TContext>>();
		if (handler == null)
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IMessageHandler<TRequestMessage, TResponse, TContext>).FullName}");

		return handler;
	}

	public override IResult<ISendResponse<TResponse>, Guid> Handle(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo<Guid> traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo<Guid>, IResult<Guid, Guid>> saveResponseMessageAction)
		=> Handle((TRequestMessage)message, (TContext)handlerContext, serviceProvider, traceInfo, saveResponseMessageAction);

	public IResult<ISendResponse<TResponse>, Guid> Handle(
		TRequestMessage message,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo<Guid> traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo<Guid>, IResult<Guid, Guid>> saveResponseMessageAction)
	{
		try
		{
			var handler = (IMessageHandler<TRequestMessage, TResponse, TContext>)CreateHandler(serviceProvider);

			IResult<TResponse, Guid> result;
			var interceptorType = handler.InterceptorType;
			if (interceptorType == null)
			{
				result = handler.Handle(message, handlerContext);
			}
			else
			{
				var interceptor = (IMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>?)serviceProvider.GetService(interceptorType);
				if (interceptor == null)
					throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>).FullName}");

				result = interceptor.InterceptHandle(message, handlerContext, handler.Handle);
			}

			var resultBuilder = new ResultBuilder<ISendResponse<TResponse>, Guid>();
			if (result.Data != null)
			{
				var response = result.Data;
				var saveResult = saveResponseMessageAction(response, handlerContext, traceInfo);
				resultBuilder.MergeHasError(saveResult);
				return resultBuilder.WithData(new SendResponse<TResponse>(handlerContext.MessageId, saveResult.Data, result.Data)).Build();
			}
			else
			{
				return resultBuilder.WithData(new SendResponse<TResponse>(handlerContext.MessageId, Guid.Empty, default)).Build();
			}
		}
		catch (Exception exHandler)
		{
			handlerContext.LogError(TraceInfo<Guid>.Create(handlerContext.TraceInfo), null, x => x.ExceptionInfo(exHandler), "Send<Messages.IRequestMessage<TResponse>> error", null);
			throw;
		}
	}
}
