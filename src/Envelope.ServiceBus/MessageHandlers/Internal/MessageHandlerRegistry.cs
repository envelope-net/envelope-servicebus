using Envelope.Exceptions;
using Envelope.Extensions;
using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.MessageHandlers.Interceptors;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Envelope.ServiceBus.MessageHandlers.Internal;

internal class MessageHandlerRegistry : IMessageRegistry, IMessageHandlerRegistry
{
	private static readonly Type _iRequestMessage = typeof(IRequestMessage<>);
	private static readonly Type _iVoidRequestMessage = typeof(IRequestMessage);
	private static readonly Type _iCommand = typeof(ICommand<>);
	private static readonly Type _iVoidCommand = typeof(ICommand);
	private static readonly Type _iQuery = typeof(IQuery<>);

	private static readonly Type _iMessageHandlerTypeDefinition = typeof(IMessageHandler<,,>);
	private static readonly Type _iAsyncMessageHandlerTypeDefinition = typeof(IAsyncMessageHandler<,,>);
	private static readonly Type _iVoidMessageHandlerTypeDefinition = typeof(IMessageHandler<,>);
	private static readonly Type _iAsyncVoidMessageHandlerTypeDefinition = typeof(IAsyncMessageHandler<,>);

	private static readonly Type _iMessageHandlerInterceptorTypeDefinition = typeof(IMessageHandlerInterceptor<,,>);
	private static readonly Type _iAsyncMessageHandlerInterceptorTypeDefinition = typeof(IAsyncMessageHandlerInterceptor<,,>);
	private static readonly Type _iVoidMessageHandlerInterceptorTypeDefinition = typeof(IMessageHandlerInterceptor<,>);
	private static readonly Type _iAsyncVoidMessageHandlerInterceptorTypeDefinition = typeof(IAsyncMessageHandlerInterceptor<,>);

	private static readonly Type _messageHandlerInterceptorTypeDefinition = typeof(MessageHandlerInterceptor<,,>);
	private static readonly Type _asyncMessageHandlerInterceptorTypeDefinition = typeof(AsyncMessageHandlerInterceptor<,,>);
	private static readonly Type _voidMessageHandlerInterceptorTypeDefinition = typeof(VoidMessageHandlerInterceptor<,>);
	private static readonly Type _asyncVoidMessageHandlerInterceptorTypeDefinition = typeof(AsyncVoidMessageHandlerInterceptor<,>);

	private readonly IServiceCollection _services;
	private readonly IMessageTypeResolver _typeResolver;
	private readonly ServiceLifetime _handlerLifetime;
	private readonly ServiceLifetime _interceptorLifetime;
	private readonly ConcurrentDictionary<Type, Type> _messageHandlersRegistry; //ConcurrentDictionary<messageType, handlerType>
	private readonly ConcurrentDictionary<Type, Type> _asyncMessageHandlersRegistry; //ConcurrentDictionary<messageType, handlerType>
	private readonly ConcurrentDictionary<Type, Type> _voidMessageHandlersRegistry; //ConcurrentDictionary<messageType, handlerType>
	private readonly ConcurrentDictionary<Type, Type> _asyncVoidMessageHandlersRegistry; //ConcurrentDictionary<messageType, handlerType>

	private readonly ConcurrentDictionary<Type, MessageType> _types; //ConcurrentDictionary<crl_message_type, MessageType>
	private readonly ConcurrentDictionary<IMessageType, Type> _messageTypes; //ConcurrentDictionary<MessageType, crl_message_type>

	private readonly ConcurrentDictionary<Type, Func<IServiceProvider, MessageHandlerContext>> _messageHandlerContextFactories;

	public MessageHandlerRegistry(
		IServiceCollection services,
		IMessageTypeResolver typeResolver,
		ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
		ServiceLifetime interceptorLifetime = ServiceLifetime.Transient)
	{
		_services = services ?? throw new ArgumentNullException(nameof(services));
		_typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
		_handlerLifetime = handlerLifetime;
		_interceptorLifetime = interceptorLifetime;
		_messageHandlersRegistry = new ConcurrentDictionary<Type, Type>();
		_asyncMessageHandlersRegistry = new ConcurrentDictionary<Type, Type>();
		_voidMessageHandlersRegistry = new ConcurrentDictionary<Type, Type>();
		_asyncVoidMessageHandlersRegistry = new ConcurrentDictionary<Type, Type>();

		_types = new ConcurrentDictionary<Type, MessageType>();
		_messageTypes = new ConcurrentDictionary<IMessageType, Type>();
		_messageHandlerContextFactories = new ConcurrentDictionary<Type, Func<IServiceProvider, MessageHandlerContext>>();
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





	public Type? GetMessageHandlerType<TRequestMessage, TResponse>()
		where TRequestMessage : IRequestMessage<TResponse>
	{
		return GetMessageHandlerType(typeof(TRequestMessage));
	}

	public Type? GetMessageHandlerType(Type messageType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		_messageHandlersRegistry.TryGetValue(messageType, out Type? handlerType);
		return handlerType;
	}

	public Type? GetAsyncMessageHandlerType<TRequestMessage, TResponse>()
		where TRequestMessage : IRequestMessage<TResponse>
	{
		return GetAsyncMessageHandlerType(typeof(TRequestMessage));
	}

	public Type? GetAsyncMessageHandlerType(Type messageType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		_asyncMessageHandlersRegistry.TryGetValue(messageType, out Type? handlerType);
		return handlerType;
	}

	public Type? GetVoidMessageHandlerType<TRequestMessage>()
		where TRequestMessage : IRequestMessage
	{
		return GetVoidMessageHandlerType(typeof(TRequestMessage));
	}

	public Type? GetVoidMessageHandlerType(Type messageType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		_voidMessageHandlersRegistry.TryGetValue(messageType, out Type? handlerType);
		return handlerType;
	}

	public Type? GetAsyncVoidMessageHandlerType<TRequestMessage>()
		where TRequestMessage : IRequestMessage
	{
		return GetAsyncVoidMessageHandlerType(typeof(TRequestMessage));
	}

	public Type? GetAsyncVoidMessageHandlerType(Type messageType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		_asyncVoidMessageHandlersRegistry.TryGetValue(messageType, out Type? handlerType);
		return handlerType;
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
					if (_iVoidMessageHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterVoidMessageHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType, contextFactory);
						return true;
					}
					else if (_iAsyncVoidMessageHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncVoidMessageHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType, contextFactory);
						return true;
					}
					else if (_iVoidMessageHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterVoidMessageHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType);
					}
					else if (_iAsyncVoidMessageHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncVoidMessageHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], type, contextType);
					}
				}
				else if (ifc.GenericTypeArguments.Length == 3)
				{
					if (_iMessageHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterMessageHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], ifc.GenericTypeArguments[2], type, contextType, contextFactory);
						return true;
					}
					else if (_iAsyncMessageHandlerTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncMessageHandler(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], ifc.GenericTypeArguments[2], type, contextType, contextFactory);
						return true;
					}
					else if (_iMessageHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterMessageHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], ifc.GenericTypeArguments[2], type, contextType);
					}
					else if (_iAsyncMessageHandlerInterceptorTypeDefinition.IsAssignableFrom(ifc.GetGenericTypeDefinition()))
					{
						RegisterAsyncMessageHandlerInterceptor(ifc.GenericTypeArguments[0], ifc.GenericTypeArguments[1], ifc.GenericTypeArguments[2], type, contextType);
					}
				}
			}
		}

		return false;
	}

	public void RegisterMessageHandler(Type messageType, Type resposeType, Type contextType, Type handlerType, Type messageHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (resposeType == null)
			throw new ArgumentNullException(nameof(resposeType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (messageHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (_messageHandlersRegistry.TryGetValue(messageType, out var registeredHandlerType))
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered to {registeredHandlerType?.FullName ?? "--NULL--"} Cannot be registered to {handlerType.FullName}");

		var iMessageType = _iRequestMessage.MakeGenericType(resposeType);
		if (!iMessageType.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the message type {messageType.FullName} must implement {iMessageType.FullName}");

		var iMessageHandlerType = _iMessageHandlerTypeDefinition.MakeGenericType(messageType, resposeType, contextType);
		if (!iMessageHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iMessageHandlerType.FullName}");

		var added = _messageHandlersRegistry.TryAdd(messageType, handlerType);
		if (!added)
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered. Cannot be registered to {handlerType.FullName}");

		var messageMetaType = MessageMetaType.RequestMessage_WithResponse;

		var iCommand = _iCommand.MakeGenericType(resposeType);
		if (!iCommand.IsAssignableFrom(messageType))
			messageMetaType = MessageMetaType.Command_WithResponse;
		else
		{
			var iQuery = _iQuery.MakeGenericType(resposeType);
			if (!iQuery.IsAssignableFrom(messageType))
				messageMetaType = MessageMetaType.Query_WithResponse;
		}

		AddMessageType(messageType, resposeType, messageMetaType, contextFactory);

		_services.Add(new ServiceDescriptor(iMessageHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterMessageHandlerInterceptor(Type messageType, Type resposeType, Type contextType, Type interceptorType, Type messageHandlerContextType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (resposeType == null)
			throw new ArgumentNullException(nameof(resposeType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_messageHandlerInterceptorTypeDefinition) && messageHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		var iMessageType = _iRequestMessage.MakeGenericType(resposeType);
		if (!iMessageType.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} message type {messageType.FullName} must implement {iMessageType.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}

	public void RegisterAsyncMessageHandler(Type messageType, Type resposeType, Type contextType, Type handlerType, Type messageHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (resposeType == null)
			throw new ArgumentNullException(nameof(resposeType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (messageHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (_asyncMessageHandlersRegistry.TryGetValue(messageType, out var registeredHandlerType))
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered to {registeredHandlerType?.FullName ?? "--NULL--"} Cannot be registered to {handlerType.FullName}");

		var iMessageType = _iRequestMessage.MakeGenericType(resposeType);
		if (!iMessageType.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the message type {messageType.FullName} must implement {iMessageType.FullName}");

		var iMessageHandlerType = _iAsyncMessageHandlerTypeDefinition.MakeGenericType(messageType, resposeType, contextType);
		if (!iMessageHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iMessageHandlerType.FullName}");

		var added = _asyncMessageHandlersRegistry.TryAdd(messageType, handlerType);
		if (!added)
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered. Cannot be registered to {handlerType.FullName}");

		var messageMetaType = MessageMetaType.RequestMessage_WithResponse;

		var iCommand = _iCommand.MakeGenericType(resposeType);
		if (!iCommand.IsAssignableFrom(messageType))
			messageMetaType = MessageMetaType.Command_WithResponse;
		else
		{
			var iQuery = _iQuery.MakeGenericType(resposeType);
			if (!iQuery.IsAssignableFrom(messageType))
				messageMetaType = MessageMetaType.Query_WithResponse;
		}

		AddMessageType(messageType, resposeType, messageMetaType, contextFactory);

		_services.Add(new ServiceDescriptor(iMessageHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterAsyncMessageHandlerInterceptor(Type messageType, Type resposeType, Type contextType, Type interceptorType, Type messageHandlerContextType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (resposeType == null)
			throw new ArgumentNullException(nameof(resposeType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_asyncMessageHandlerInterceptorTypeDefinition) && messageHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		var iMessageType = _iRequestMessage.MakeGenericType(resposeType);
		if (!iMessageType.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} message type {messageType.FullName} must implement {iMessageType.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}

	public void RegisterVoidMessageHandler(Type messageType, Type contextType, Type handlerType, Type messageHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (messageHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (_voidMessageHandlersRegistry.TryGetValue(messageType, out var registeredHandlerType))
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered to {registeredHandlerType?.FullName ?? "--NULL--"} Cannot be registered to {handlerType.FullName}");

		if (!_iVoidRequestMessage.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the message type {messageType.FullName} must implement {_iVoidRequestMessage.FullName}");

		var iMessageHandlerType = _iVoidMessageHandlerTypeDefinition.MakeGenericType(messageType, contextType);
		if (!iMessageHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iMessageHandlerType.FullName}");

		var added = _voidMessageHandlersRegistry.TryAdd(messageType, handlerType);
		if (!added)
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered. Cannot be registered to {handlerType.FullName}");

		var messageMetaType = MessageMetaType.RequestMessage_Void;

		if (!_iVoidCommand.IsAssignableFrom(messageType))
			messageMetaType = MessageMetaType.Command_Void;

		AddMessageType(messageType, null, messageMetaType, contextFactory);

		_services.Add(new ServiceDescriptor(iMessageHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterVoidMessageHandlerInterceptor(Type messageType, Type contextType, Type interceptorType, Type messageHandlerContextType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_voidMessageHandlerInterceptorTypeDefinition) && messageHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iVoidRequestMessage.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} message type {messageType.FullName} must implement {_iVoidRequestMessage.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}

	public void RegisterAsyncVoidMessageHandler(Type messageType, Type contextType, Type handlerType, Type messageHandlerContextType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (handlerType == null)
			throw new ArgumentNullException(nameof(handlerType));

		if (messageHandlerContextType != contextType)
			throw new ConfigurationException($"For handler {handlerType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (_asyncVoidMessageHandlersRegistry.TryGetValue(messageType, out var registeredHandlerType))
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered to {registeredHandlerType?.FullName ?? "--NULL--"} Cannot be registered to {handlerType.FullName}");

		if (!_iVoidRequestMessage.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For handler {handlerType.FullName} the message type {messageType.FullName} must implement {_iVoidRequestMessage.FullName}");

		var iMessageHandlerType = _iAsyncVoidMessageHandlerTypeDefinition.MakeGenericType(messageType, contextType);
		if (!iMessageHandlerType.IsAssignableFrom(handlerType))
			throw new ConfigurationException($"Handler type {handlerType.FullName} must implement {iMessageHandlerType.FullName}");

		var added = _asyncVoidMessageHandlersRegistry.TryAdd(messageType, handlerType);
		if (!added)
			throw new ConfigurationException($"Message type {messageType.FullName} is already registered. Cannot be registered to {handlerType.FullName}");

		var messageMetaType = MessageMetaType.RequestMessage_Void;

		if (!_iVoidCommand.IsAssignableFrom(messageType))
			messageMetaType = MessageMetaType.Command_Void;

		AddMessageType(messageType, null, messageMetaType, contextFactory);

		_services.Add(new ServiceDescriptor(iMessageHandlerType, handlerType, _handlerLifetime));
	}

	public void RegisterAsyncVoidMessageHandlerInterceptor(Type messageType, Type contextType, Type interceptorType, Type messageHandlerContextType)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));
		if (contextType == null)
			throw new ArgumentNullException(nameof(contextType));
		if (interceptorType == null)
			throw new ArgumentNullException(nameof(interceptorType));

		if (!interceptorType.Inherits(_asyncVoidMessageHandlerInterceptorTypeDefinition) && messageHandlerContextType != contextType)
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} is required context type {messageHandlerContextType.FullName} but found {contextType.FullName}");

		if (!_iVoidRequestMessage.IsAssignableFrom(messageType))
			throw new ConfigurationException($"For interceptor {interceptorType.FullName} message type {messageType.FullName} must implement {_iVoidRequestMessage.FullName}");

		_services.Add(new ServiceDescriptor(interceptorType, interceptorType, _interceptorLifetime));
	}









	











	private void AddMessageType(Type type, Type? responseType, MessageMetaType messageMetaType, Func<IServiceProvider, MessageHandlerContext> contextFactory)
	{
		var resolvedTypeString = _typeResolver.ToName(type);
		if (string.IsNullOrWhiteSpace(resolvedTypeString))
			throw new ConfigurationException($"Message type {type?.FullName} {nameof(resolvedTypeString)} == NULL");

		var messageType =
			new MessageType
			(
				type.FullName ?? type.ToFriendlyFullName() ?? type.Name,
				resolvedTypeString,
				messageMetaType
			);

		IMessageType? previousResponseType = null;
		var added = _types.TryAdd(type, messageType);
		if (added)
		{
			_messageTypes.TryAdd(messageType, type);
			_messageHandlerContextFactories.TryAdd(type, contextFactory);
		}
		else
		{
			previousResponseType = _types[type].ResponseMessageType;
		}

		if (responseType != null)
		{
			resolvedTypeString = _typeResolver.ToName(responseType);
			if (string.IsNullOrWhiteSpace(resolvedTypeString))
				throw new ConfigurationException($"Response type's {responseType} FullName == NULL");

			var responseMessageMetaType = messageMetaType switch
			{
				MessageMetaType.RequestMessage_WithResponse => MessageMetaType.Response_ForRequestMessage,
				MessageMetaType.Command_WithResponse => MessageMetaType.Response_ForCommand,
				MessageMetaType.Query_WithResponse => MessageMetaType.Response_ForQuery,
				_ => throw new ConfigurationException($"Type {type.FullName} cannot have any response. {nameof(messageMetaType)} = {messageMetaType}"),
			};

			if (added)
			{
				if (previousResponseType != null)
				{
					if (!previousResponseType.CrlType.Equals(resolvedTypeString, StringComparison.OrdinalIgnoreCase))
						throw new ConfigurationException($"Message type {type.FullName} previously registered with {nameof(responseType)} == {previousResponseType.CrlType} | Current {nameof(responseType)} == {resolvedTypeString}");

					if (previousResponseType.MessageMetaType != responseMessageMetaType)
						throw new ConfigurationException($"Message type {type.FullName} previously registered with {nameof(previousResponseType.MessageMetaType)} == {previousResponseType.MessageMetaType} | Current {nameof(responseMessageMetaType)} == {responseMessageMetaType}");
				}
				//else
				//{
				//	throw new ConfigurationException($"Message type {type.FullName} previously registered with no {nameof(responseType)} | Current {nameof(responseType)} == {resolvedTypeString}");
				//}

				return;
			}

			var responseMessageType =
				new MessageType
				(
					responseType.FullName ?? responseType.ToFriendlyFullName() ?? responseType.Name,
					resolvedTypeString,
					responseMessageMetaType
				);

			added = _types.TryAdd(responseType, responseMessageType);
			if (added)
				_messageTypes.TryAdd(responseMessageType, responseType);
		}
	}

	public IMessageHandlerContext? CreateMessageHandlerContext(Type messageType, IServiceProvider serviceProvider)
	{
		if (_messageHandlerContextFactories.TryGetValue(messageType, out var contextFactory))
			return contextFactory(serviceProvider);

		return null;
	}
}
