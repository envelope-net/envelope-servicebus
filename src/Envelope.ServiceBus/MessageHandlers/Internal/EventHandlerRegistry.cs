using Envelope.Exceptions;
using Envelope.Extensions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.MessageHandlers.Internal;

internal class EventHandlerRegistry : IEventRegistry, IEventHandlerRegistry
{
	private static readonly Type _iEvent = typeof(IEvent);

	private static readonly Type _iEventHandlerTypeDefinition = typeof(IEventHandler<,>);
	private static readonly Type _iAsyncEventHandlerTypeDefinition = typeof(IAsyncEventHandler<,>);

	private static readonly Type _iEventHandlerInterceptorTypeDefinition = typeof(IEventHandlerInterceptor<,>);
	private static readonly Type _iAsyncEventHandlerInterceptorTypeDefinition = typeof(IAsyncEventHandlerInterceptor<,>);

	private static readonly Type _eventHandlerInterceptorTypeDefinition = typeof(EventHandlerInterceptor<,>);
	private static readonly Type _asyncEventHandlerInterceptorTypeDefinition = typeof(AsyncEventHandlerInterceptor<,>);

	private readonly IServiceCollection _services;
	private readonly IMessageTypeResolver _typeResolver;
	private readonly ServiceLifetime _handlerLifetime;
	private readonly ServiceLifetime _interceptorLifetime;
	private readonly ConcurrentDictionary<Type, List<Type>> _eventHandlersRegistry; //ConcurrentDictionary<eventType, List<handlerType>>
	private readonly ConcurrentDictionary<Type, List<Type>> _asyncEventHandlersRegistry; //ConcurrentDictionary<eventType, List<handlerType>>

	private readonly ConcurrentDictionary<Type, MessageType> _types; //ConcurrentDictionary<crl_message_type, MessageType>
	private readonly ConcurrentDictionary<IMessageType, Type> _messageTypes; //ConcurrentDictionary<MessageType, crl_message_type>

	private readonly ConcurrentDictionary<Type, Func<IServiceProvider, MessageHandlerContext>> _eventHandlerContextFactories;

	public EventHandlerRegistry(
		IServiceCollection services,
		IMessageTypeResolver typeResolver,
		ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
		ServiceLifetime interceptorLifetime = ServiceLifetime.Transient)
	{
		_services = services ?? throw new ArgumentNullException(nameof(services));
		_typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
		_handlerLifetime = handlerLifetime;
		_interceptorLifetime = interceptorLifetime;
		_eventHandlersRegistry = new ConcurrentDictionary<Type, List<Type>>();
		_asyncEventHandlersRegistry = new ConcurrentDictionary<Type, List<Type>>();

		_types = new ConcurrentDictionary<Type, MessageType>();
		_messageTypes = new ConcurrentDictionary<IMessageType, Type>();
		_eventHandlerContextFactories = new ConcurrentDictionary<Type, Func<IServiceProvider, MessageHandlerContext>>();
	}

	public IEnumerable<IMessageType>? GetAllMessageTypes()
		=> _types.Values.ToList();

	public IMessageType? GetMessageType(Type type)
	{
		_types.TryGetValue(type, out var messageType);
		return messageType;
	}

	public Type? GetType(IMessageType messageType)
	{
		_messageTypes.TryGetValue(messageType, out var type);
		return type;
	}





	public List<Type>? GetEventHandlerType<TEvent>()
		where TEvent : IEvent
	{
		return GetEventHandlerType(typeof(TEvent));
	}

	public List<Type>? GetEventHandlerType(Type eventType)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));

		_eventHandlersRegistry.TryGetValue(eventType, out List<Type>? handlerTypes);
		return handlerTypes;
	}

	public List<Type>? GetAsyncEventHandlerType<TEvent>()
		where TEvent : IEvent
	{
		return GetAsyncEventHandlerType(typeof(TEvent));
	}

	public List<Type>? GetAsyncEventHandlerType(Type eventType)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));

		_asyncEventHandlersRegistry.TryGetValue(eventType, out List<Type>? handlerTypes);
		return handlerTypes;
	}




	public bool TryRegisterHandlerAndInterceptor(Type type, Type contextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		var interfaces = type.GetInterfaces();
		if (interfaces == null)
			return false;

		foreach (var ifc in interfaces)
		{
			if (ifc.IsGenericType)
			{
				if (ifc.GenericTypeArguments.Length == 2)
				{
					if (_iEventHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterEventHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType, contextFactory);
						return true;
					}
					else if (_iAsyncEventHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncEventHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType, contextFactory);
						return true;
					}
					else if (_iEventHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterEventHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType);
					}
					else if (_iAsyncEventHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncEventHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType);
					}
				}
			}
		}

		return false;
	}










	public void RegisterEventHandler(Type eventType, Type contextType, Type handlerType, Type eventHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (eventHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {eventHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iEvent.IsAssignableFrom(eventType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the event type {eventType.FullName} must implement {_iEvent.FullName}");

		var iEventHandlerType = _iEventHandlerTypeDefinition.MakeGenericType(eventType, contextType);
		if (!iEventHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iEventHandlerType.FullName}");

		var added = _eventHandlersRegistry.AddOrUpdate(eventType, new List<Type> { handlerType }, (key, existingTypes) =>
		{
			existingTypes.Add(handlerType);
			return existingTypes;
		});

		AddEventType(eventType, MessageMetaType.Event, contextFactory);

		_services.Add(new ServiceDescriptor(iEventHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterEventHandlerInterceptor(Type eventType, Type contextType, Type interceptorType, Type eventHandlerContextType)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_eventHandlerInterceptorTypeDefinition) && eventHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {eventHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iEvent.IsAssignableFrom(eventType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} event type {eventType.FullName} must implement {_iEvent.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}

	public void RegisterAsyncEventHandler(Type eventType, Type contextType, Type handlerType, Type eventHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (eventHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {eventHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iEvent.IsAssignableFrom(eventType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the event type {eventType.FullName} must implement {_iEvent.FullName}");

		var iEventHandlerType = _iAsyncEventHandlerTypeDefinition.MakeGenericType(eventType, contextType);
		if (!iEventHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iEventHandlerType.FullName}");

		var added = _asyncEventHandlersRegistry.AddOrUpdate(eventType, new List<Type> { handlerType }, (key, existingTypes) =>
		{
			existingTypes.Add(handlerType);
			return existingTypes;
		});

		AddEventType(eventType, MessageMetaType.Event, contextFactory);

		_services.Add(new ServiceDescriptor(iEventHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterAsyncEventHandlerInterceptor(Type eventType, Type contextType, Type interceptorType, Type eventHandlerContextType)
	{
		if (eventType == null)
			throw new ArgumentNullException(nameof(eventType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_asyncEventHandlerInterceptorTypeDefinition) && eventHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {eventHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iEvent.IsAssignableFrom(eventType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} event type {eventType.FullName} must implement {_iEvent.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}











	private void AddEventType(Type eventType, MessageMetaType messageMetaType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		var resolvedTypeString = _typeResolver.ToName(eventType);
		if (string.IsNullOrWhiteSpace(resolvedTypeString))
			throw new ConfigurationException($"Message type {eventType?.FullName} {nameof(resolvedTypeString)} == NULL");

		var messageType =
			new MessageType
			(
				eventType.FullName ?? eventType.ToFriendlyFullName() ?? eventType.Name,
				resolvedTypeString,
				messageMetaType
			);

		var added = _types.TryAdd(eventType, messageType);
		if (added)
		{
			_messageTypes.TryAdd(messageType, eventType);
			_eventHandlerContextFactories.TryAdd(eventType, contextFactory);
		}
	}

	public IMessageHandlerContext? CreateEventHandlerContext(Type eventType, IServiceProvider serviceProvider)
	{
		if (_eventHandlerContextFactories.TryGetValue(eventType, out var contextFactory))
			return contextFactory(serviceProvider);

		return null;
	}
}
