using Envelope.ServiceBus.Configuration;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Queues.Configuration;

public class QueueProviderConfiguration : IQueueProviderConfiguration, IValidable
{
	public IServiceBusOptions ServiceBusOptions { get; }

	internal Dictionary<string, Func<IServiceProvider, IMessageQueue>> MessageQueues { get; }
	Dictionary<string, Func<IServiceProvider, IMessageQueue>> IQueueProviderConfiguration.MessageQueuesInternal => MessageQueues;

	public Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public QueueProviderConfiguration(IServiceBusOptions serviceBusOptions)
	{
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(serviceBusOptions));
		MessageQueues = new Dictionary<string, Func<IServiceProvider, IMessageQueue>>();
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (FaultQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FaultQueue))} == null"));
		}

		if (MessageQueues.Count == 0)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageQueues))} == null"));
		}

		return parentErrorBuffer;
	}
}
