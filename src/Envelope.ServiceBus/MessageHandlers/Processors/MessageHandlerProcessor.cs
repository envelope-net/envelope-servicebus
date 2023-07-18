using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class MessageHandlerProcessor<TResponse> : MessageHandlerProcessorBase
{
	public abstract IResult<TResponse> Handle(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		string? unhandledExceptionDetail);

	public abstract void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		Messages.IRequestMessage<TResponse>? message,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider);
}

internal class MessageHandlerProcessor<TRequestMessage, TResponse, TContext> : MessageHandlerProcessor<TResponse>
	where TRequestMessage : Messages.IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	protected override IMessageHandler CreateHandler(IServiceProvider serviceProvider)
	{
		var handler = serviceProvider.GetService<IMessageHandler<TRequestMessage, TResponse, TContext>>()
			?? throw new InvalidOperationException($"Could not resolve handler for {typeof(IMessageHandler<TRequestMessage, TResponse, TContext>).FullName}");

		return handler;
	}

	public override IResult<TResponse> Handle(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		string? unhandledExceptionDetail)
		=> Handle((TRequestMessage)message, (TContext)handlerContext, serviceProvider, traceInfo, unhandledExceptionDetail);

	public IResult<TResponse> Handle(
		TRequestMessage message,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		string? unhandledExceptionDetail)
	{
		IMessageHandler<TRequestMessage, TResponse, TContext>? handler = null;

		try
		{
			handler = (IMessageHandler<TRequestMessage, TResponse, TContext>)CreateHandler(serviceProvider);

			IResult<TResponse> result;
			var interceptorType = handler.InterceptorType;
			if (interceptorType == null)
			{
				result = handler.Handle(message, handlerContext);
			}
			else
			{
				var interceptor = (IMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>?)serviceProvider.GetService(interceptorType)
					?? throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>).FullName}");

				result = interceptor.InterceptHandle(message, handlerContext, handler.Handle);
			}

			var resultBuilder = new ResultBuilder<TResponse>();
			resultBuilder.MergeAll(result);

			IResult<TResponse> newResult;
			if (result.Data != null)
			{
				newResult = resultBuilder.WithData(result.Data).Build();
			}
			else
			{
				newResult = resultBuilder.WithData(default).Build();
			}

			if (newResult.HasError)
			{
				try
				{
					handler.OnError(traceInfo, null, newResult, unhandledExceptionDetail, message, handlerContext);
				}
				catch (Exception onErrorEx)
				{
					try
					{
						handlerContext.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnError: Send<Messages.IRequestMessage<TResponse>> error", null);
					}
					catch { }
				}
			}

			return newResult;
		}
		catch (Exception exHandler)
		{
			traceInfo = TraceInfo.Create(traceInfo);
			try
			{
				handlerContext.LogError(traceInfo, x => x.ExceptionInfo(exHandler), "Send<Messages.IRequestMessage<TResponse>> error", null);
			}
			catch { }

			if (handler != null)
			{
				try
				{
					handler.OnError(traceInfo, exHandler, null, unhandledExceptionDetail, message, handlerContext);
				}
				catch (Exception onErrorEx)
				{
					try
					{
						handlerContext.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnError: Send<Messages.IRequestMessage<TResponse>> error", null);
					}
					catch { }
				}
			}

			throw;
		}
	}

	public override void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		Messages.IRequestMessage<TResponse>? message,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider)
		=> OnError(traceInfo, exception, errorResult, detail, (TRequestMessage?)message, (TContext?)handlerContext, serviceProvider);

	public void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		TRequestMessage? message,
		TContext? handlerContext,
		IServiceProvider serviceProvider)
	{
		try
		{
			var handler = (IMessageHandler<TRequestMessage, TResponse, TContext>)CreateHandler(serviceProvider);
			handler.OnError(traceInfo, exception, errorResult, detail, message, handlerContext);
		}
		catch (Exception onErrorEx)
		{
			try
			{
				handlerContext?.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage> error", null);
			}
			catch { }
		}
	}
}
