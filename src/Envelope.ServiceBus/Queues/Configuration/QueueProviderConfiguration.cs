using Envelope.ServiceBus.Configuration;
using Envelope.Text;
using System.Text;

namespace Envelope.ServiceBus.Queues.Configuration;

public class QueueProviderConfiguration : IQueueProviderConfiguration
{
	public IServiceBusOptions ServiceBusOptions { get; }

	internal Dictionary<string, Func<IServiceProvider, IMessageQueue>> MessageQueues { get; }
	Dictionary<string, Func<IServiceProvider, IMessageQueue>> IQueueProviderConfiguration.MessageQueues => MessageQueues;

	public Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public QueueProviderConfiguration(IServiceBusOptions serviceBusOptions)
	{
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(serviceBusOptions));
		MessageQueues = new Dictionary<string, Func<IServiceProvider, IMessageQueue>>();
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (FaultQueue == null)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(FaultQueue))} == null");
		}

		if (MessageQueues.Count == 0)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MessageQueues))} == null");
		}

		return parentErrorBuffer;
	}
}
