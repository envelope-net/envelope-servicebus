using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class EventHandlerProcessor : EventHandlerProcessorBase
{
	public abstract IResult Handle(
		IEvent @vent,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail);

	public abstract void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		IEvent? @vent,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider);
}

internal class EventHandlerProcessor<TEvent, TContext> : EventHandlerProcessor
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	protected override IEnumerable<IEventHandler> CreateHandlers(IServiceProvider serviceProvider)
	{
		var handlers = serviceProvider.GetServices<IEventHandler<TEvent, TContext>>();
		if (handlers == null || !handlers.Any())
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IEventHandler<TEvent, TContext>).FullName}");

		return handlers;
	}

	public override IResult Handle(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail)
		=> Handle((TEvent)@event, (TContext)handlerContext, serviceProvider, unhandledExceptionDetail);

	public IResult Handle(
		TEvent @event,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		string? unhandledExceptionDetail)
	{
		List<IEventHandler<TEvent, TContext>>? handlers = null;
		try
		{
			handlers = CreateHandlers(serviceProvider).Select(x => (IEventHandler<TEvent, TContext>)x).ToList();
		}
		catch (Exception exHandler)
		{
			handlerContext.LogError(TraceInfo.Create(handlerContext.TraceInfo), x => x.ExceptionInfo(exHandler), $"PublishAsync<IEvent> {nameof(CreateHandlers)} error", null);
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
					result = handler.Handle(@event, handlerContext);
				}
				else
				{
					var interceptor = (IEventHandlerInterceptor<TEvent, TContext>?)serviceProvider.GetService(interceptorType)
						?? throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IEventHandlerInterceptor<TEvent, TContext>).FullName}");

					result = interceptor.InterceptHandle(@event, handlerContext, handler.Handle);
				}

				resultBuilder.MergeAll(result);

				if (resultBuilder.HasError())
				{
					var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);
					try
					{
						handler.OnError(traceInfo, null, resultBuilder.Build(), unhandledExceptionDetail, @event, handlerContext);
					}
					catch (Exception onErrorEx)
					{
						try
						{
							handlerContext.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnError: PublishAsync<IEvent> error", null);
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
					handlerContext.LogError(traceInfo, x => x.ExceptionInfo(exHandler), "PublishAsync<IEvent> error", null);
				}
				catch { }

				if (handler != null)
				{
					try
					{
						handler.OnError(traceInfo, exHandler, null, unhandledExceptionDetail, @event, handlerContext);
					}
					catch (Exception onErrorEx)
					{
						try
						{
							handlerContext.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnError: PublishAsync<IEvent> error", null);
						}
						catch { }
					}
				}

				throw;
			}
		}

		return resultBuilder.Build();
	}

	public override void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		IEvent? @event,
		IMessageHandlerContext? handlerContext,
		IServiceProvider serviceProvider)
		=> OnError(traceInfo, exception, errorResult, detail, (TEvent?)@event, (TContext?)handlerContext, serviceProvider);

	public void OnError(
		ITraceInfo traceInfo,
		Exception? exception,
		IResult? errorResult,
		string? detail,
		TEvent? @event,
		TContext? handlerContext,
		IServiceProvider serviceProvider)
	{
		try
		{
			var handlers = CreateHandlers(serviceProvider).Select(x => (IEventHandler<TEvent, TContext>)x).ToList();

			foreach (var handler in handlers)
			{
				try
				{
					handler.OnError(traceInfo, exception, errorResult, detail, @event, handlerContext);
				}
				catch (Exception handlerOnErrorEx)
				{
					try
					{
						handlerContext?.LogCritical(traceInfo, x => x.ExceptionInfo(handlerOnErrorEx), "OnErrorAsync1: SendAsync<Messages.IRequestMessage> error", null);
					}
					catch { }
				}
			}
		}
		catch (Exception onErrorEx)
		{
			try
			{
				handlerContext?.LogCritical(traceInfo, x => x.ExceptionInfo(onErrorEx), "OnErrorAsync2: SendAsync<Messages.IRequestMessage> error", null);
			}
			catch { }
		}
	}
}
