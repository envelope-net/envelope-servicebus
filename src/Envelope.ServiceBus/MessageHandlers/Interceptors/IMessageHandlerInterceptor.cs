using Envelope.ServiceBus.Messages;
using Envelope.Services;

namespace Envelope.ServiceBus.MessageHandlers.Interceptors;

/// <summary>
/// Defines a base interceptor for message handlers
/// </summary>
public interface IMessageHandlerInterceptor
{
}

/// <summary>
/// Defines an interceptor for message handlers
/// </summary>
/// <typeparam name="TRequestMessage">The type of request message being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IMessageHandlerInterceptor<TRequestMessage, TResponse, TContext> : IMessageHandlerInterceptor
	where TRequestMessage : IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Intercepts the message handler handle method
	/// </summary>
	IResult<TResponse, Guid> InterceptHandle(TRequestMessage message, TContext handlerContext, Func<TRequestMessage, TContext, IResult<TResponse, Guid>> next);
}

/// <summary>
/// Defines an interceptor for message handlers
/// </summary>
/// <typeparam name="TRequestMessage">The type of request message being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IAsyncMessageHandlerInterceptor<TRequestMessage, TResponse, TContext> : IMessageHandlerInterceptor
	where TRequestMessage : IRequestMessage<TResponse>
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Intercepts the message handler handle method
	/// </summary>
	Task<IResult<TResponse, Guid>> InterceptHandleAsync(TRequestMessage message, TContext handlerContext, Func<TRequestMessage, TContext, CancellationToken, Task<IResult<TResponse, Guid>>> next, CancellationToken cancellationToken);
}

/// <summary>
/// Defines an interceptor for message handlers
/// </summary>
/// <typeparam name="TRequestMessage">The type of request message being handled</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IMessageHandlerInterceptor<TRequestMessage, TContext> : IMessageHandlerInterceptor
	where TRequestMessage : IRequestMessage
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Intercepts the message handler handle method
	/// </summary>
	IResult<Guid> InterceptHandle(TRequestMessage message, TContext handlerContext, Func<TRequestMessage, TContext, IResult<Guid>> next);
}

/// <summary>
/// Defines an interceptor for message handlers
/// </summary>
/// <typeparam name="TRequestMessage">The type of request message being handled</typeparam>
/// <typeparam name="TContext">The type of <see cref="IMessageHandlerContext"/></typeparam>
public interface IAsyncMessageHandlerInterceptor<TRequestMessage, TContext> : IMessageHandlerInterceptor
	where TRequestMessage : IRequestMessage
	where TContext : IMessageHandlerContext
{
	/// <summary>
	/// Intercepts the message handler handle method
	/// </summary>
	Task<IResult<Guid>> InterceptHandleAsync(TRequestMessage message, TContext handlerContext, Func<TRequestMessage, TContext, CancellationToken, Task<IResult<Guid>>> next, CancellationToken cancellationToken);
}
