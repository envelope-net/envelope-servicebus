using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class EventHandlerProcessor : EventHandlerProcessorBase
{
	public abstract IResult<Guid> Handle(
		IEvent @vent,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider);
}

internal class EventHandlerProcessor<TEvent, TContext> : EventHandlerProcessor
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	protected override IEnumerable<IEventHandler> CreateHandlers(IServiceProvider serviceProvider, bool throwNoHandlerException)
	{
		var handlers = serviceProvider.GetServices<IEventHandler<TEvent, TContext>>();
		if (handlers == null || (throwNoHandlerException && !handlers.Any()))
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IEventHandler<TEvent, TContext>).FullName}");

		return handlers;
	}

	public override IResult<Guid> Handle(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider)
		=> Handle((TEvent)@event, (TContext)handlerContext, serviceProvider);

	public IResult<Guid> Handle(
		TEvent @event,
		TContext handlerContext,
		IServiceProvider serviceProvider)
	{
		List<IEventHandler<TEvent, TContext>>? handlers = null;
		try
		{
			handlers = CreateHandlers(serviceProvider, handlerContext.ThrowNoHandlerException).Select(x => (IEventHandler<TEvent, TContext>)x).ToList();
		}
		catch (Exception exHandler)
		{
			handlerContext.LogError(TraceInfo<Guid>.Create(handlerContext.TraceInfo), null, x => x.ExceptionInfo(exHandler), $"PublishAsync<IEvent> {nameof(CreateHandlers)} error", null);
			throw;
		}

		var resultBuilder = new ResultBuilder<Guid>();
		IResult<Guid>? result = null;
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
					var interceptor = (IEventHandlerInterceptor<TEvent, TContext>?)serviceProvider.GetService(interceptorType);
					if (interceptor == null)
						throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IEventHandlerInterceptor<TEvent, TContext>).FullName}");

					result = interceptor.InterceptHandle(@event, handlerContext, handler.Handle);
				}

				resultBuilder.Merge(result);
			}
			catch (Exception exHandler)
			{
				handlerContext.LogError(TraceInfo<Guid>.Create(handlerContext.TraceInfo), null, x => x.ExceptionInfo(exHandler), "PublishAsync<IEvent> error", null);
				throw;
			}
		}

		return resultBuilder.Build();
	}
}
