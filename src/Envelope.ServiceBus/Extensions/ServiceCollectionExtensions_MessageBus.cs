using Envelope.Extensions;
using Envelope.MessageBus.Configuration;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.Messages.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Envelope.ServiceBus.Extensions;

public static partial class ServiceCollectionExtensions
{
	public static IServiceCollection AddInMemoryMessageBus(
		this IServiceCollection services,
		Action<MessageBusConfigurationBuilder> configure,
		ServiceLifetime messageBusLifetime = ServiceLifetime.Scoped,
		ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
		ServiceLifetime interceptorLifetime = ServiceLifetime.Transient)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var builder = MessageBusConfigurationBuilder.GetDefaultBuilder();
		configure(builder);

		var config = builder.Build();

		var registry = new MessageHandlerRegistry(
			services,
			config.MessageTypeResolver,
			handlerLifetime,
			interceptorLifetime);

		if (config.MessageHandlerTypes != null)
		{
			foreach (var messageHandlerType in config.MessageHandlerTypes)
			{
				registry.TryRegisterHandlerAndInterceptor(messageHandlerType.HandlerType, messageHandlerType.ContextType, messageHandlerType.ContextFactory);

				if (messageHandlerType.HandlerInterceptorType != null)
					registry.TryRegisterHandlerAndInterceptor(messageHandlerType.HandlerInterceptorType, messageHandlerType.ContextType, messageHandlerType.ContextFactory);
			}
		}

		if (config.MessageHandlerAssemblies != null)
		{
			foreach (var kvp in config.MessageHandlerAssemblies.GroupBy(x => x.HandlersAssembly).ToDictionary(x => x.Key, x => x.First()))
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
			typeof(IMessageBus),
			sp =>
			{
				var options = builder.Build().BuildOptions(sp);
				var messageBus = new MessageBus(sp, options, registry);
				return messageBus;
			},
			messageBusLifetime));

		return services;
	}

	public static IServiceCollection AddInMemoryMessageBus(
		this IServiceCollection services,
		string messageBusName,
		params IMessageHandlersAssembly[] assembliesToScan)
	{
		if (string.IsNullOrWhiteSpace(messageBusName))
			throw new ArgumentNullException(nameof(messageBusName));

		if (assembliesToScan == null || assembliesToScan.Length == 0)
			throw new ArgumentNullException(nameof(assembliesToScan));

		return AddInMemoryMessageBus(
			services,
			builder =>
			{
				builder
					.MessageBusName(messageBusName)
					.MessageHandlerAssemblies(assembliesToScan)
					.MessageTypeResolver(new FullNameTypeResolver());
			},
			ServiceLifetime.Scoped,
			ServiceLifetime.Transient,
			ServiceLifetime.Transient);
	}
}
