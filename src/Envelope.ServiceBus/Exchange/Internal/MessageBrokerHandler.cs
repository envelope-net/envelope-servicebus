using Envelope.ServiceBus.Exchange.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;

namespace Envelope.ServiceBus.Exchange.Internal;

internal class MessageBrokerHandler<TMessage> : IMessageBrokerHandler<TMessage>
	where TMessage : class, IMessage
{
	public async Task<MessageHandlerResult> HandleAsync(IExchangeMessage<TMessage> message, ExchangeContext<TMessage> exchangeContext, CancellationToken cancellationToken)
	{
		var traceInfo = TraceInfo<Guid>.Create(exchangeContext.ServiceBusOptions.HostInfo.HostName);
		var result = new ResultBuilder<Guid>();

		var messageHandlerResultFactory = exchangeContext.ServiceBusOptions.MessageHandlerResultFactory;

		var queue = exchangeContext.ServiceBusOptions.QueueProvider.GetQueue<TMessage>(message.TargetQueueName!);
		if (queue == null)
		{
			result.WithInvalidOperationException(traceInfo, $"Target Queue with name '{message.TargetQueueName}' does not exists.");
			return messageHandlerResultFactory.Error(result.Build());
		}

		bool disableFaultQueue = false;
		try
		{
			var context = exchangeContext.ServiceBusOptions.QueueProvider.CreateQueueEnqueueContext(traceInfo, message);
			disableFaultQueue = context.DisableFaultQueue;
			var enqueueResult = await queue.EnqueueAsync(message.Message, context, cancellationToken);
			if (result.MergeHasError(enqueueResult))
			{
				return messageHandlerResultFactory.Error(result.Build());
			}
			else
			{
				return messageHandlerResultFactory.DeliveredInternal();
			}
		}
		catch (Exception ex)
		{
			var errorMessage = 
				exchangeContext.ServiceBusOptions.HostLogger.LogError(
					traceInfo,
					exchangeContext.ServiceBusOptions.HostInfo,
					HostStatus.Unchanged,
					x => x
						.ExceptionInfo(ex)
						.Detail($"{nameof(message.ExchangeName)} == {message.ExchangeName} | {nameof(message.TargetQueueName)} == {message.TargetQueueName} | MessageType = '{message.Message?.GetType().FullName}'"),
					$"{nameof(HandleAsync)}<{nameof(TMessage)}>",
					null);

			if (!disableFaultQueue)
			{
				try
				{
					var faultContext = exchangeContext.ServiceBusOptions.QueueProvider.CreateFaultQueueContext(traceInfo, message);
					await exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync(message.Message, faultContext, cancellationToken);
				}
				catch (Exception faultEx)
				{
					exchangeContext.ServiceBusOptions.HostLogger.LogError(
						traceInfo,
						exchangeContext.ServiceBusOptions.HostInfo,
						HostStatus.Unchanged,
						x => x
							.ExceptionInfo(faultEx)
							.Detail($"{nameof(message.ExchangeName)} == {message.ExchangeName} | {nameof(message.TargetQueueName)} == {message.TargetQueueName} | MessageType = {message.Message?.GetType().FullName} >> {nameof(exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue)}.{nameof(exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue.EnqueueAsync)}"),
						$"{nameof(HandleAsync)}<{nameof(TMessage)}> >> {nameof(exchangeContext.ServiceBusOptions.QueueProvider.FaultQueue)}",
						null);
				}
			}

			result.WithError(errorMessage);
			return messageHandlerResultFactory.AbortedInternal(result.Build());
		}
	}
}
