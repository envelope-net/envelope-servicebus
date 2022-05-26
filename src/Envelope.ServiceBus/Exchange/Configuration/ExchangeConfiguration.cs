using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Exchange.Routing;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Queues;
using Envelope.Text;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Exchange.Configuration;

public class ExchangeConfiguration<TMessage> : IExchangeConfiguration<TMessage>, IValidable
		where TMessage : class, IMessage
{
	/// <inheritdoc/>
	public IServiceBusOptions ServiceBusOptions { get; }

	/// <inheritdoc/>
	public string ExchangeName { get; set; }

	/// <inheritdoc/>
	public QueueType QueueType { get; set; }

	/// <inheritdoc/>
	public TimeSpan? StartDelay { get; set; }

	/// <inheritdoc/>
	public TimeSpan FetchInterval { get; set; }

	/// <inheritdoc/>
	public int? MaxSize { get; set; }

	public Func<IServiceProvider, IExchangeMessageFactory<TMessage>> ExchangeMessageFactory { get; set; }

	public Func<IServiceProvider, IMessageBrokerHandler<TMessage>> MessageBrokerHandler { get; set; }

	public Func<IServiceProvider, IQueue> FIFOQueue { get; set; }

	public Func<IServiceProvider, IQueue> DelayableQueue { get; set; }

	/// <inheritdoc/>
	public Func<IServiceProvider, IMessageBodyProvider> MessageBodyProvider { get; set; }

	/// <inheritdoc/>
	public Func<IServiceProvider, IExhcangeRouter> Router { get; set; }

	public Func<IServiceProvider, IErrorHandlingController>? ErrorHandling { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public ExchangeConfiguration(IServiceBusOptions serviceBusOptions)
	{
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(serviceBusOptions));
	}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (ServiceBusOptions == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ServiceBusOptions))} == null");
		}

		if (string.IsNullOrWhiteSpace(ExchangeName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeName))} == null");
		}

		if (StartDelay < TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(StartDelay))} is invalid");
		}

		if (FetchInterval <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FetchInterval))} is invalid");
		}

		if (MaxSize < 1)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MaxSize))} is invalid");
		}

		if (ExchangeMessageFactory == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeMessageFactory))} == null");
		}

		if (MessageBrokerHandler == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageBrokerHandler))} == null");
		}

		if (FIFOQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FIFOQueue))} == null");
		}

		if (DelayableQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DelayableQueue))} == null");
		}

		if (MessageBodyProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageBodyProvider))} == null");
		}

		if (Router == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(Router))} == null");
		}

		return parentErrorBuffer;
	}
}
