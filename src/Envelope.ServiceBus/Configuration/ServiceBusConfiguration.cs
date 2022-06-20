using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues;
using Envelope.ServiceBus.Queues.Configuration;
using Envelope.Text;
using Envelope.Transactions;
using System.Text;

namespace Envelope.ServiceBus.Configuration;

public class ServiceBusConfiguration : IServiceBusConfiguration
{
	public IHostInfo HostInfo { get; set; }
	public string ServiceBusName { get; set; }
	public Func<IServiceProvider, IMessageTypeResolver> MessageTypeResolver { get; set; }
	public Func<IServiceProvider, IHostLogger> HostLogger { get; set; }
	public Func<IServiceProvider, ITransactionManagerFactory> TransactionManagerFactory { get; set; }
	public Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; set; }
	public Action<ExchangeProviderConfigurationBuilder> ExchangeProviderConfiguration { get; set; }
	public Action<QueueProviderConfigurationBuilder> QueueProviderConfiguration { get; set; }
	public Type MessageHandlerContextType { get; set; }
	public Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; set; }
	public Func<IServiceProvider, IHandlerLogger> HandlerLogger { get; set; }
	public List<ServiceBusEventHandler> ServiceBusEventHandlers { get; }
	public Func<IServiceProvider, IFaultQueue>? OrchestrationEventsFaultQueue { get; set; }
	public Action<ExchangeConfigurationBuilder<OrchestrationEvent>>? OrchestrationExchange { get; set; }
	public Action<MessageQueueConfigurationBuilder<OrchestrationEvent>>? OrchestrationQueue { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public ServiceBusConfiguration()
	{
		ServiceBusEventHandlers = new List<ServiceBusEventHandler>();
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (HostInfo == null && string.IsNullOrWhiteSpace(ServiceBusName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ServiceBusName))} == null");
		}

		if (MessageTypeResolver == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageTypeResolver))} == null");
		}

		if (HostLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostLogger))} == null");
		}

		if (TransactionManagerFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionManagerFactory))} == null");
		}

		if (TransactionContextFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(TransactionContextFactory))} == null");
		}

		//kvoli tomu, ze sa neskor automaticky prida exchange pre orchestracie
		//if (ExchangeProviderConfiguration == null)
		//{
		//	if (parentErrorBuffer == null)
		//		parentErrorBuffer = new StringBuilder();

		//	parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeProviderConfiguration))} == null");
		//}

		//kvoli tomu, ze sa neskor automaticky prida queue pre orchestracie
		//if (QueueProviderConfiguration == null)
		//{
		//	if (parentErrorBuffer == null)
		//		parentErrorBuffer = new StringBuilder();

		//	parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(QueueProviderConfiguration))} == null");
		//}

		if (MessageHandlerContextType == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerContextType))} == null");
		}

		if (MessageHandlerContextFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerContextFactory))} == null");
		}

		if (HandlerLogger == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HandlerLogger))} == null");
		}

		return parentErrorBuffer;
	}
}
