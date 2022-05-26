﻿using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Queues;
using Envelope.Text;
using System.Text;

namespace Envelope.ServiceBus.Exchange.Configuration;

public class ExchangeProviderConfiguration : IExchangeProviderConfiguration
{
	public IServiceBusOptions ServiceBusOptions { get; }

	internal Dictionary<string, Func<IServiceProvider, IExchange>> Exchanges { get; }
	Dictionary<string, Func<IServiceProvider, IExchange>> IExchangeProviderConfiguration.Exchanges => Exchanges;

	public Func<IServiceProvider, IFaultQueue> FaultQueue { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ExchangeProviderConfiguration(IServiceBusOptions serviceBusOptions)
	{
		ServiceBusOptions = serviceBusOptions ?? throw new ArgumentNullException(nameof(serviceBusOptions));
		Exchanges = new Dictionary<string, Func<IServiceProvider, IExchange>>();
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

		if (Exchanges.Count == 0)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(Exchanges))} == null");
		}

		return parentErrorBuffer;
	}
}
