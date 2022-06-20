using Envelope.EventBus.Configuration;
using Envelope.Extensions;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.Messages.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Envelope.ServiceBus.Extensions;

public static partial class ServiceCollectionExtensions
{
	public static IServiceCollection AddInMemoryEventBus(
		this IServiceCollection services,
		Action<EventBusConfigurationBuilder> configure,
		ServiceLifetime eventBusLifetime = ServiceLifetime.Scoped,
		ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
		ServiceLifetime interceptorLifetime = ServiceLifetime.Transient)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = EventBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);

		//var orchestrationEventHandlerType = typeof(Orchestrations.EventHandlers.Internal.OrchestrationEventHandler);

		//builder.AddEventHandlerType(
		//	new EventHandlerType<Orchestrations.EventHandlers.Internal.OrchestrationEventHandler, Orchestrations.EventHandlers.OrchestrationEventHandlerContext>(
		//		typeof(Orchestrations.EventHandlers.AsyncEventHandlerInterceptor<Orchestrations.Model.OrchestrationEvent>),
		//		sp => new Orchestrations.EventHandlers.OrchestrationEventHandlerContext()));

		var config = builder.Build();

		var registry = new EventHandlerRegistry(
			services,
			config.EventTypeResolver,
			handlerLifetime,
			interceptorLifetime);

		if (config.EventHandlerTypes != null)
		{
			foreach (var eventHandlerType in config.EventHandlerTypes)
			{
				registry.TryRegisterHandlerAndInterceptor(eventHandlerType.HandlerType, eventHandlerType.ContextType, eventHandlerType.ContextFactory);

				if (eventHandlerType.HandlerInterceptorType != null)
					registry.TryRegisterHandlerAndInterceptor(eventHandlerType.HandlerInterceptorType, eventHandlerType.ContextType, eventHandlerType.ContextFactory);
			}
		}

		if (config.EventHandlerAssemblies != null)
		{
			foreach (var kvp in config.EventHandlerAssemblies.GroupBy(x => x.HandlersAssembly).ToDictionary(x => x.Key, x => x.First()))
			{
				var assembly = kvp.Key;

				var typesToScan =
					assembly.DefinedTypes
						.Where(type => type.IsInstanceable());

				foreach (var typeInfo in typesToScan)
					registry.TryRegisterHandlerAndInterceptor(typeInfo, kvp.Value.ContextType, kvp.Value.ContextFactory);
			}
		}

		services.TryAddSingleton<IMessageHandlerResultFactory, MessageHandlerResultFactory>();

		services.TryAdd(new ServiceDescriptor(
			typeof(IEventBus),
			sp =>
			{
				var builder = EventBusConfigurationBuilder.GetDefaultBuilder();
				configure(builder);

				var cfg = builder.Build();
				var messageBus = new EventBus(sp, cfg, registry);
				return messageBus;
			},
			eventBusLifetime));

		services.TryAdd(new ServiceDescriptor(
			typeof(IEventPublisher),
			sp => sp.GetRequiredService<IEventBus>(),
			eventBusLifetime));

		return services;
	}

	public static IServiceCollection AddInMemoryEventBus(
		this IServiceCollection services,
		string messageBusName,
		params IEventHandlersAssembly[] assembliesToScan)
	{
		if (string.IsNullOrWhiteSpace(messageBusName))
			throw new ArgumentNullException(nameof(messageBusName));

		if (assembliesToScan == null || assembliesToScan.Length == 0)
			throw new ArgumentNullException(nameof(assembliesToScan));

		return AddInMemoryEventBus(
			services,
			builder =>
			{
				builder
					.EventBusName(messageBusName)
					.EventHandlerAssemblies(assembliesToScan)
					.EventTypeResolver(new FullNameTypeResolver());
			},
			ServiceLifetime.Scoped,
			ServiceLifetime.Transient,
			ServiceLifetime.Transient);
	}
}
