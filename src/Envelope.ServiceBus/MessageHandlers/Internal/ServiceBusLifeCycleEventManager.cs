using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Model;
using Envelope.Trace;

namespace Envelope.ServiceBus.MessageHandlers.Internal;

internal class ServiceBusLifeCycleEventManager : IServiceBusLifeCycleEventManager
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public event ServiceBusEventHandler OnServiceBusEvent;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public Task PublishServiceBusEventInternalAsync(IServiceBusEvent serviceBusEvent, ITraceInfo<Guid> traceInfo, IServiceBusOptions serviceBusOptions)
	{
		if (OnServiceBusEvent != null)
		{
			if (serviceBusOptions == null)
				throw new ArgumentNullException(nameof(serviceBusOptions));

			traceInfo = TraceInfo<Guid>.Create(traceInfo);

			try
			{
				_ = Task.Run(async () => await OnServiceBusEvent.Invoke(serviceBusEvent, traceInfo));

				//await OnServiceBusEvent.Invoke(serviceBusEvent, traceInfo);
			}
			catch (Exception ex)
			{
				return serviceBusOptions.HostLogger.LogErrorAsync(
					traceInfo,
					serviceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(ex),
					$"{nameof(serviceBusEvent)} type = {serviceBusEvent.GetType().FullName}",
					null,
					default);
			}
		}

		return Task.CompletedTask;
	}
}
