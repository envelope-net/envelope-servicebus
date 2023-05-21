using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class AsyncEventHandlerProcessor : EventHandlerProcessorBase
{
	public abstract Task<IResult> HandleAsync(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default);

	public abstract Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		IEvent? @event,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default);
}

internal class AsyncEventHandlerProcessor<TEvent, TContext> : AsyncEventHandlerProcessor
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	protected override IEnumerable<IEventHandler> CreateHandlers(IServiceProvider serviceProvider)
	{
		var handlers = serviceProvider.GetServices<IAsyncEventHandler<TEvent, TContext>>();
		if (handlers == null || !handlers.Any())
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IAsyncEventHandler<TEvent, TContext>).FullName}");

		return handlers;
	}

	public override Task<IResult> HandleAsync(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
		=> HandleAsync((TEvent)@event, (TContext)handlerContext, serviceProvider, unhandledExceptionDetail, cancellationToken);

	public async Task<IResult> HandleAsync(
		TEvent @event,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail,
		CancellationToken cancellationToken = default)
	{
		List<IAsyncEventHandler<TEvent, TContext>>? handlers = null;
		try
		{
			handlers = CreateHandlers(serviceProvider).Select(x => (IAsyncEventHandler<TEvent, TContext>)x).ToList();
		}
		catch (Exception exHandler)
		{
			await handlerContext.LogErrorAsync(TraceInfo.Create(handlerContext.TraceInfo), x => x.ExceptionInfo(exHandler), $"PublishAsync<IEvent> {nameof(CreateHandlers)} error", null, cancellationToken).ConfigureAwait(false);
			throw;
		}

		var resultBuilder = new ResultBuilder();
		IResult? result = null;
		foreach (var handler in handlers)
		{
			try
			{
				var interceptorType = handler.InterceptorType;
				if (interceptorType == null)
				{
					result = await handler.HandleAsync(@event, handlerContext, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					var interceptor = (IAsyncEventHandlerInterceptor<TEvent, TContext>?)serviceProvider.GetService(interceptorType)
						?? throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IAsyncEventHandlerInterceptor<TEvent, TContext>).FullName}");

					result = await interceptor.InterceptHandleAsync(@event, handlerContext, handler.HandleAsync, cancellationToken).ConfigureAwait(false);
				}

				resultBuilder.Merge(result);

				if (resultBuilder.HasError())
				{
					var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);
					try
					{
						await handler.OnErrorAsync(traceInfo, null, resultBuilder.Build(), unhandledExceptionDetail, @event, handlerContext, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception onErrorEx)
					{
						try
						{
							await handlerContext.LogErrorAsync(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: PublishAsync<IEvent> error", null, cancellationToken).ConfigureAwait(false);
						}
						catch { }
					}
				}
			}
			catch (Exception exHandler)
			{
				var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);
				try
				{
					await handlerContext.LogErrorAsync(traceInfo, x => x.ExceptionInfo(exHandler), "PublishAsync<IEvent> error", null, cancellationToken).ConfigureAwait(false);
				}
				catch { }

				if (handler != null)
				{
					try
					{
						await handler.OnErrorAsync(traceInfo, exHandler, null, unhandledExceptionDetail, @event, handlerContext, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception onErrorEx)
					{
						try
						{
							await handlerContext.LogErrorAsync(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync: PublishAsync<IEvent> error", null, cancellationToken).ConfigureAwait(false);
						}
						catch { }
					}
				}

				throw;
			}
		}

		return resultBuilder.Build();
	}

	public override Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		IEvent? @event,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
		=> OnErrorAsync(traceInfo, exception, errorResult, detail, (TEvent?)@event, (TContext?)handlerContext, serviceProvider, cancellationToken);

	public async Task OnErrorAsync(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		TEvent? @event,
		TContext? handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var handlers = CreateHandlers(serviceProvider).Select(x => (IAsyncEventHandler<TEvent, TContext>)x).ToList();

			foreach (var handler in handlers)
			{
				try
				{
					await handler.OnErrorAsync(traceInfo, exception, errorResult, detail, @event, handlerContext, cancellationToken);
				}
				catch (Exception handlerOnErrorEx)
				{
					try
					{
						if (handlerContext != null)
							handlerContext.LogCritical(traceInfo, x => x.ExceptionInfo(handlerOnErrorEx), "OnErrorAsync1: PublishAsync<IEvent> error", null);
					}
					catch { }
				}
			}
		}
		catch (Exception onErrorEx)
		{
			try
			{
				if (handlerContext != null)
					await handlerContext.LogCriticalAsync(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync2: PublishAsync<IEvent> error", null, cancellationToken).ConfigureAwait(false);
			}
			catch { }
		}
	}
}
