using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

public class MessageQueueConfiguration<TMessage> : IMessageQueueConfiguration<TMessage>, IValidable
		where TMessage : class, IMessage
{
	/// <inheritdoc/>
	public IServiceBusOptions ServiceBusOptions { get; }

	/// <inheritdoc/>
	public string QueueName { get; set; }

	/// <inheritdoc/>
	public QueueType QueueType { get; set; }

	/// <inheritdoc/>
	public bool IsPull { get; set; }

	/// <inheritdoc/>
	public TimeSpan? StartDelay { get; set; }

	/// <inheritdoc/>
	public TimeSpan FetchInterval { get; set; }

	/// <inheritdoc/>
	public int? MaxSize { get; set; }

	/// <inheritdoc/>
	public TimeSpan? DefaultProcessingTimeout { get; set; }

	public Func<IServiceProvider, int?, IQueue<IQueuedMessage<TMessage>>> FIFOQueue { get; set; }

	public Func<IServiceProvider, int?, IQueue<IQueuedMessage<TMessage>>> DelayableQueue { get; set; }

	/// <inheritdoc/>
	public Func<IServiceProvider, IMessageBodyProvider> MessageBodyProvider { get; set; }

	public Func<IServiceProvider, IServiceBusOptions, HandleMessage<TMessage>>? MessageHandler { get; set; }

	public Func<IServiceProvider, IErrorHandlingController>? ErrorHandling { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public MessageQueueConfiguration(IServiceBusOptions serviceBusOptions)
	{
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(serviceBusOptions));
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(QueueName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(QueueName))} == null"));
		}

		if (StartDelay < TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(StartDelay))} is invalid"));
		}

		if (FetchInterval <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FetchInterval))} is invalid"));
		}

		if (MaxSize < 1)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MaxSize))} is invalid"));
		}

		if (DefaultProcessingTimeout <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DefaultProcessingTimeout))} is invalid"));
		}

		if (FIFOQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FIFOQueue))} == null"));
		}

		if (DelayableQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DelayableQueue))} == null"));
		}

		if (MessageBodyProvider == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageBodyProvider))} == null"));
		}

		if (!IsPull && MessageHandler == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageHandler))} == null"));
		}

		return parentErrorBuffer;
	}
}
