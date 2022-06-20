using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Hosts.Logging;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.MessageHandlers.Internal;
using Envelope.ServiceBus.MessageHandlers.Logging;
using Envelope.ServiceBus.Messages.Resolvers;
using Envelope.ServiceBus.Queues;
using Envelope.Text;
using Envelope.Transactions;
using System.Text;

namespace Envelope.ServiceBus.Configuration.Internal;

internal class ServiceBusOptions : IServiceBusOptions
{
	public IServiceProvider ServiceProvider { get; }
	public IHostInfo HostInfo { get; set; }
	public IMessageTypeResolver MessageTypeResolver { get; set; }
	public IHostLogger HostLogger { get; set; }
	public ITransactionManagerFactory TransactionManagerFactory { get; set; }
	public Func<IServiceProvider, ITransactionManager, Task<ITransactionContext>> TransactionContextFactory { get; set; }
	public IExchangeProvider ExchangeProvider { get; set; }
	public IQueueProvider QueueProvider { get; set; }
	public Type MessageHandlerContextType { get; set; }
	public Func<IServiceProvider, MessageHandlerContext> MessageHandlerContextFactory { get; set; }
	public IHandlerLogger HandlerLogger { get; set; }
	public IMessageHandlerResultFactory MessageHandlerResultFactory { get; set; }
	public IServiceBusLifeCycleEventManager ServiceBusLifeCycleEventManager { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ServiceBusOptions(IServiceProvider serviceProvider)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		ServiceBusLifeCycleEventManager = new ServiceBusLifeCycleEventManager();
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (ServiceProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ServiceProvider))} == null");
		}

		if (HostInfo == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(HostInfo))} == null");
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

		if (ExchangeProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeProvider))} == null");
		}

		if (QueueProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(QueueProvider))} == null");
		}

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

		if (MessageHandlerResultFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandlerResultFactory))} == null");
		}

		return parentErrorBuffer;
	}
}
