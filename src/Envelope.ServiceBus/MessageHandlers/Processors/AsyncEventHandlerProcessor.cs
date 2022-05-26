using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.MessageHandlers.Processors;

internal abstract class AsyncEventHandlerProcessor : EventHandlerProcessorBase
{
	public abstract Task<IResult<Guid>> HandleAsync(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default);
}

internal class AsyncEventHandlerProcessor<TEvent, TContext> : AsyncEventHandlerProcessor
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	protected override IEnumerable<IEventHandler> CreateHandlers(IServiceProvider serviceProvider, bool throwNoHandlerException)
	{
		var handlers = serviceProvider.GetServices<IAsyncEventHandler<TEvent, TContext>>();
		if (handlers == null || (throwNoHandlerException && !handlers.Any()))
			throw new InvalidOperationException($"Could not resolve handler for {typeof(IAsyncEventHandler<TEvent, TContext>).FullName}");

		return handlers;
	}

	public override Task<IResult<Guid>> HandleAsync(
		IEvent @event,
		IMessageHandlerContext handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
		=> HandleAsync((TEvent)@event, (TContext)handlerContext, serviceProvider, cancellationToken);

	public async Task<IResult<Guid>> HandleAsync(
		TEvent @event,
		TContext handlerContext,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		List<IAsyncEventHandler<TEvent, TContext>>? handlers = null;
		try
		{
			handlers = CreateHandlers(serviceProvider, handlerContext.ThrowNoHandlerException).Select(x => (IAsyncEventHandler<TEvent, TContext>)x).ToList();
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
					result = await handler.HandleAsync(@event, handlerContext, cancellationToken);
				}
				else
				{
					var interceptor = (IAsyncEventHandlerInterceptor<TEvent, TContext>?)serviceProvider.GetService(interceptorType);
					if (interceptor == null)
						throw new InvalidOperationException($"Could not resolve interceptor for {typeof(IAsyncEventHandlerInterceptor<TEvent, TContext>).FullName}");

					result = await interceptor.InterceptHandleAsync(@event, handlerContext, handler.HandleAsync, cancellationToken);
				}

				resultBuilder.Merge(result);
			}
			catch (Exception exHandler)
			{
				await handlerContext.LogErrorAsync(TraceInfo<Guid>.Create(handlerContext.TraceInfo), null, x => x.ExceptionInfo(exHandler), "PublishAsync<IEvent> error", null, cancellationToken);
				throw;
			}
		}

		return resultBuilder.Build();
	}
}
