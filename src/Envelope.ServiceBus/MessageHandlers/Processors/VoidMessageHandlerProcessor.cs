using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class VoidMessageHandlerProcessor : MessageHandlerProcessorBase
{
	public abstract IResult Handle(
		Messages.IRequestMessage message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider);
}

internal class VoidMessageHandlerProcessor<TRequestMessage, TContext> : VoidMessageHandlerProcessor
	where TRequestMessage : Messages.IRequestMessage
	where TContext : IMessageHandlerContext
{
	protected override IMessageHandler CreateHandler(IServiceProvider serviceProvider)
	{
		var handler = serviceProvider.GetService<IMessageHandler<TRequestMessage, TContext>>();
		if (handler == null)
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IMessageHandler<TRequestMessage, TContext>).FullName}");

		return handler;
	}

	public override IResult Handle(
		Messages.IRequestMessage message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider)
		=> Handle((TRequestMessage)message, (TContext)handlerContext, serviceProvider);

	public IResult Handle(
		TRequestMessage message,
		TContext handlerContext,
		IServiceProvider serviceProvider)
	{
		try
		{
			var handler = (IMessageHandler<TRequestMessage, TContext>)CreateHandler(serviceProvider);

			IResult result;
			var interceptorType = handler.InterceptorType;
			if (interceptorType == null)
			{
				result = handler.Handle(message, handlerContext);
			}
			else
			{
				var interceptor = (IMessageHandlerInterceptor<TRequestMessage, TContext>?)serviceProvider.GetService(interceptorType);
				if (interceptor == null)
					throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IMessageHandlerInterceptor<TRequestMessage, TContext>).FullName}");

				result = interceptor.InterceptHandle(message, handlerContext, handler.Handle);
			}

			return result;
		}
		catch (Exception exHandler)
		{
			handlerContext.LogError(TraceInfo.Create(handlerContext.TraceInfo), null, x => x.ExceptionInfo(exHandler), "Send<Messages.IRequestMessage> error", null);
			throw;
		}
	}
}
