using System.Reflection;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlersAssembly
{
	Assembly HandlersAssembly { get; }
	Type ContextType { get; }
	Func<IServiceProvider, MessageHandlerContext> ContextFactory { get; }
}

public class MessageHandlersAssembly<TContext> : IMessageHandlersAssembly
	where TContext : MessageHandlerContext
{
	public Assembly HandlersAssembly { get; set; }
	public Type ContextType => typeof(TContext);
	public Func<IServiceProvider, TContext> ContextFactory { get; set; }
	Func<IServiceProvider, MessageHandlerContext> IMessageHandlersAssembly.ContextFactory => ContextFactory;

	public MessageHandlersAssembly(Assembly messageHandlersAssembly, Func<IServiceProvider, TContext> factory)
	{
		HandlersAssembly = messageHandlersAssembly ?? throw new ArgumentNullException(nameof(messageHandlersAssembly));
		ContextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
	}
}
