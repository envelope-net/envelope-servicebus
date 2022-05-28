using Envelope.Services;
using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.MessageHandlers;

/// <summary>
/// Defines a base handler for events
/// </summary>
public interface IEventHandler
{
}

/// <summary>
/// Defines a handler for an event
/// </summary>
/// <typeparam name="TEvent">The type of event being handled</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IEventHandler<TEvent, TContext> : IEventHandler
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Interceptor for handle method. Interceptor must implement <see cref="IEventHandlerInterceptor{TEvent, TContext}"/>
	/// </summary>
	Type? InterceptorType { get; set; }

	/// <summary>
	/// Handles an event
	/// </summary>
	/// <returns>Response from the event</returns>
	IResult Handle(TEvent @event, TContext handlerContext);
}

/// <summary>
/// Defines a handler for an event
/// </summary>
/// <typeparam name="TEvent">The type of event being handled</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IAsyncEventHandler<TEvent, TContext> : IEventHandler
	where TEvent : IEvent
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Interceptor for handle method. Interceptor must implement <see cref="IAsyncEventHandlerInterceptor{TEvent, TContext}"/>
	/// </summary>
	Type? InterceptorType { get; set; }

	/// <summary>
	/// Handles an event
	/// </summary>
	/// <returns>Response from the event</returns>
	Task<IResult> HandleAsync(TEvent @event, TContext handlerContext, CancellationToken cancellationToken = default);
}
