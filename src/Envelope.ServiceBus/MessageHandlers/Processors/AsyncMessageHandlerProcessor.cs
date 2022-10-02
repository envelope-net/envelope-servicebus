using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Internal;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class AsyncMessageHandlerProcessor<TResponse> : MessageHandlerProcessorBase
{
	public abstract Task<IResult<ISendResponse<TResponse>>> HandleAsync(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo, CancellationToken, Task<IResult<Guid>>> saveResponseMessageAction,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default);

	public abstract Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		Messages.IRequestMessage<TResponse>? message,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default);
}

internal class AsyncMessageHandlerProcessor<TRequestMessage, TResponse, TContext> : AsyncMessageHandlerProcessor<TResponse>
	where TRequestMessage : Messages.IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	protected override IMessageHandler CreateHandler(IServiceProvider serviceProvider)
	{
		var handler = serviceProvider.GetService<IAsyncMessageHandler<TRequestMessage, TResponse, TContext>>();
		if (handler == null)
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IAsyncMessageHandler<TRequestMessage, TResponse, TContext>).FullName}");

		return handler;
	}

	public override Task<IResult<ISendResponse<TResponse>>> HandleAsync(
		Messages.IRequestMessage<TResponse> message,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo, CancellationToken, Task<IResult<Guid>>> saveResponseMessageAction,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
		=> HandleAsync((TRequestMessage)message, (TContext)handlerContext, serviceProvider, traceInfo, saveResponseMessageAction, unhandledExceptionDetail, cancellationToken);

	public async Task<IResult<ISendResponse<TResponse>>> HandleAsync(
		TRequestMessage message,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		ITraceInfo traceInfo,
		Func<TResponse, IMessageHandlerContext, ITraceInfo, CancellationToken, Task<IResult<Guid>>> saveResponseMessageAction,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
	{
		IAsyncMessageHandler<TRequestMessage, TResponse, TContext>? handler = null;

		try
		{
			handler = (IAsyncMessageHandler<TRequestMessage, TResponse, TContext>)CreateHandler(serviceProvider);

			IResult<TResponse> result;
			var interceptorType = handler.InterceptorType;
			if (interceptorType == null)
			{
				result = await handler.HandleAsync(message, handlerContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var interceptor = (IAsyncMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>?)serviceProvider.GetService(interceptorType);
				if (interceptor == null)
					throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IAsyncMessageHandlerInterceptor<TRequestMessage, TResponse, TContext>).FullName}");

				result = await interceptor.InterceptHandleAsync(message, handlerContext, handler.HandleAsync, cancellationToken).ConfigureAwait(false);
			}

			var resultBuilder = new ResultBuilder<ISendResponse<TResponse>>();
			resultBuilder.Merge(result);

			IResult<ISendResponse<TResponse>> newResult;
			if (result.Data != null)
			{
				var response = result.Data;
				var saveResult = await saveResponseMessageAction(response, handlerContext, traceInfo, cancellationToken).ConfigureAwait(false);
				resultBuilder.MergeHasError(saveResult);
				newResult = resultBuilder.WithData(new SendResponse<TResponse>(handlerContext.MessageId, saveResult.Data, result.Data)).Build();
			}
			else
			{
				newResult = resultBuilder.WithData(new SendResponse<TResponse>(handlerContext.MessageId, Guid.Empty, default)).Build();
			}

			if (newResult.HasError)
			{
				try
				{
					await handler.OnErrorAsync(traceInfo, null, newResult, unhandledExceptionDetail, message, handlerContext, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception onErrorEx)
				{
					try
					{
						await handlerContext.LogCriticalAsync(traceInfo, null, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage<TResponse>> error", null, cancellationToken).ConfigureAwait(false);
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
				await handlerContext.LogErrorAsync(traceInfo, null, x => x.ExceptionInfo(exHandler), "SendAsync<Messages.IRequestMessage<TResponse>> error", null, cancellationToken).ConfigureAwait(false);
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
						await handlerContext.LogCriticalAsync(traceInfo, null, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: SendAsync<Messages.IRequestMessage<TResponse>> error", null, cancellationToken).ConfigureAwait(false);
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
		Messages.IRequestMessage<TResponse>? message,
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
			var handler = (IAsyncMessageHandler<TRequestMessage, TResponse, TContext>)CreateHandler(serviceProvider);
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
