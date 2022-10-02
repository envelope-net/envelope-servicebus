using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class AsyncVoidMessageHandlerProcessor : MessageHandlerProcessorBase
{
	public abstract Task<IResult> HandleAsync(
		Messages.IRequestMessage message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default);

	public abstract Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		Messages.IRequestMessage? message,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default);
}

internal class AsyncVoidMessageHandlerProcessor<TRequestMessage, TContext> : AsyncVoidMessageHandlerProcessor
	where TRequestMessage : Messages.IRequestMessage
	where TContext : IMessageHandlerContext
{
	protected override IMessageHandler CreateHandler(IServiceProvider serviceProvider)
	{
		var handler = serviceProvider.GetService<IAsyncMessageHandler<TRequestMessage, TContext>>();
		if (handler == null)
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IAsyncMessageHandler<TRequestMessage, TContext>).FullName}");

		return handler;
	}

	public override Task<IResult> HandleAsync(
		Messages.IRequestMessage message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
		=> HandleAsync((TRequestMessage)message, (TContext)handlerContext, serviceProvider, unhandledExceptionDetail, cancellationToken);

	public async Task<IResult> HandleAsync(
		TRequestMessage message,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
	{
		IAsyncMessageHandler<TRequestMessage, TContext>? handler = null;

		try
		{
			handler = (IAsyncMessageHandler<TRequestMessage, TContext>)CreateHandler(serviceProvider);

			IResult result;
			var interceptorType = handler.InterceptorType;
			if (interceptorType == null)
			{
				result = await handler.HandleAsync(message, handlerContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var interceptor = (IAsyncMessageHandlerInterceptor<TRequestMessage, TContext>?)serviceProvider.GetService(interceptorType);
				if (interceptor == null)
					throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IAsyncMessageHandlerInterceptor<TRequestMessage, TContext>).FullName}");

				result = await interceptor.InterceptHandleAsync(message, handlerContext, handler.HandleAsync, cancellationToken).ConfigureAwait(false);
			}

			if (result.HasError)
			{
				var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);
				try
				{
					await handler.OnErrorAsync(traceInfo, null, result, unhandledExceptionDetail, message, handlerContext, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception onErrorEx)
				{
					try
					{
						await handlerContext.LogCriticalAsync(traceInfo, null, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage> error", null, cancellationToken).ConfigureAwait(false);
					}
					catch { }
				}
			}

			return result;
		}
		catch (Exception exHandler)
		{
			var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);
			try
			{
				await handlerContext.LogErrorAsync(traceInfo, null, x => x.ExceptionInfo(exHandler), "SendAsync<Messages.IRequestMessage> error", null, cancellationToken).ConfigureAwait(false);
			}
			catch { }

			if (handler != null)
			{
				try
				{
					await handler.OnErrorAsync(traceInfo, exHandler, null, unhandledExceptionDetail, message, handlerContext, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception onErrorEx)
				{
					try
					{
						await handlerContext.LogCriticalAsync(traceInfo, null, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage> error", null, cancellationToken).ConfigureAwait(false);
					}
					catch { }
				}
			}

			throw;
		}
	}

	public override Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		Messages.IRequestMessage? message,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
		=> OnErrorAsync(traceInfo, exception, errorResult, detail, (TRequestMessage?)message, (TContext?)handlerContext, serviceProvider, cancellationToken);

	public async Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		TRequestMessage? message,
		TContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var handler = (IAsyncMessageHandler<TRequestMessage, TContext>)CreateHandler(serviceProvider);
			await handler.OnErrorAsync(traceInfo, exception, errorResult, detail, message, handlerContext, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception onErrorEx)
		{
			try
			{
				if (handlerContext != null)
					await handlerContext.LogCriticalAsync(traceInfo, null, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage> error", null, cancellationToken).ConfigureAwait(false);
			}
			catch { }
		}
	}
}
