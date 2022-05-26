using System.Reflection;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IEventHandlersAssembly
{
	Assembly HandlersAssembly { get; }
	Type ContextType { get; }
	Func<IServiceProvider, MessageHandlerContext> ContextFactory { get; }
}

public class EventHandlersAssembly<TContext> : IEventHandlersAssembly
	where TContext : MessageHandlerContext
{
	public Assembly HandlersAssembly { get; set; }
	public Type ContextType => typeof(TContext);
	public Func<IServiceProvider, TContext> ContextFactory { get; set; }
	Func<IServiceProvider, MessageHandlerContext> IEventHandlersAssembly.ContextFactory => ContextFactory;

	public EventHandlersAssembly(Assembly eventHandlersAssembly, Func<IServiceProvider, TContext> factory)
	{
		HandlersAssembly = eventHandlersAssembly ?? throw new ArgumentNullException(nameof(eventHandlersAssembly));
		ContextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
	}
}
