//using Envelope.ServiceBus.Messages;
//using Microsoft.Extensions.Logging;

//namespace Envelope.ServiceBus.Orchestrations.EventHandlers;

//internal class AsyncEventHandlerInterceptor<TEvent> : Envelope.ServiceBus.MessageHandlers.Interceptors.AsyncEventHandlerInterceptor<TEvent, OrchestrationEventHandlerContext>
//	where TEvent : IEvent
//{
//	public AsyncEventHandlerInterceptor(ILogger<AsyncEventHandlerInterceptor<TEvent>> logger)
//		: base(logger)
//	{
//	}

//	//public override async Task<IResult<Guid>> InterceptHandleAsync(TEvent message, OrchestrationEventHandlerContext handlerContext, Func<TEvent, OrchestrationEventHandlerContext, CancellationToken, Task<IResult<Guid>>> next, CancellationToken cancellationToken)
//	//{
//	//	var result = await base.InterceptHandleAsync(message, handlerContext, next, cancellationToken);
//	//	return result;
//	//}
//}
