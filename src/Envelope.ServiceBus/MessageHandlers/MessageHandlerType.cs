namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlerType
{
	Type HandlerType { get; }
	Type? HandlerInterceptorType { get; }
	Type ContextType { get; }
	Func<IServiceProvider, MessageHandlerContext> ContextFactory { get; }
}

public class MessageHandlerType<TContext> : IMessageHandlerType
	where TContext : MessageHandlerContext
{
	public Type HandlerType { get; set; }
	public Type? HandlerInterceptorType { get; set; }
	public Type ContextType => typeof(TContext);
	public Func<IServiceProvider, TContext> ContextFactory { get; set; }
	Func<IServiceProvider, MessageHandlerContext> IMessageHandlerType.ContextFactory => ContextFactory;

	public MessageHandlerType(Type messageHandlerType, Type? handlerInterceptorType, Func<IServiceProvider, TContext> factory)
	{
		HandlerType = messageHandlerType ?? throw new ArgumentNullException(nameof(messageHandlerType));
		HandlerInterceptorType = handlerInterceptorType;
		ContextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
	}
}

public class MessageHandlerType<TMessageHandlerType, TContext> : MessageHandlerType<TContext>, IMessageHandlerType
	where TContext : MessageHandlerContext
{
	public MessageHandlerType(Func<IServiceProvider, TContext> factory)
		: base(typeof(TMessageHandlerType), null, factory)
	{
	}

	public MessageHandlerType(Type? handlerInterceptorType, Func<IServiceProvider, TContext> factory)
		: base(typeof(TMessageHandlerType), handlerInterceptorType, factory)
	{
	}
}

