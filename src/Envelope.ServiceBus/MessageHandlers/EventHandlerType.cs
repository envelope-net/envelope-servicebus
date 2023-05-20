namespace Envelope.ServiceBus.MessageHandlers;

public interface IEventHandlerType
{
	Type HandlerType { get; }
	Type? HandlerInterceptorType { get; }
	Type ContextType { get; }
	Func<IServiceProvider, MessageHandlerContext> ContextFactory { get; }
}

public class EventHandlerType<TContext> : IEventHandlerType
	where TContext : MessageHandlerContext
{
	public Type HandlerType { get; set; }
	public Type? HandlerInterceptorType { get; set; }
	public Type ContextType => typeof(TContext);
	public Func<IServiceProvider, TContext> ContextFactory { get; set; }
	Func<IServiceProvider, MessageHandlerContext> IEventHandlerType.ContextFactory => ContextFactory;

	public EventHandlerType(Type eventHandlerType, Type? handlerInterceptorType, Func<IServiceProvider, TContext> factory)
	{
		HandlerType = eventHandlerType ?? throw new ArgumentNullException(nameof(eventHandlerType));
		HandlerInterceptorType = handlerInterceptorType;
		ContextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
	}
}

public class EventHandlerType<TEventHandlerType, TContext> : EventHandlerType<TContext>, IEventHandlerType
	where TContext : MessageHandlerContext
{
	public EventHandlerType(Func<IServiceProvider, TContext> factory)
		: base(typeof(TEventHandlerType), null, factory)
	{
	}

	public EventHandlerType(Type? handlerInterceptorType, Func<IServiceProvider, TContext> factory)
		: base(typeof(TEventHandlerType), handlerInterceptorType, factory)
	{
	}
}
