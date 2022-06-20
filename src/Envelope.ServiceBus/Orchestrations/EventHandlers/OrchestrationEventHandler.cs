//using Envelope.ServiceBus.MessageHandlers;
//using Envelope.ServiceBus.Orchestrations.Model;
//using Envelope.ServiceBus.Orchestrations.Persistence;
//using Envelope.Services;
//using Envelope.Trace;

//namespace Envelope.ServiceBus.Orchestrations.EventHandlers.Internal;

//internal class OrchestrationEventHandler : IAsyncEventHandler<OrchestrationEvent, OrchestrationEventHandlerContext>, IDisposable
//{
//	public Type? InterceptorType { get; set; } = typeof(AsyncEventHandlerInterceptor<OrchestrationEvent>);

//	private readonly IOrchestrationRepository _orchestrationRepository;

//	public OrchestrationEventHandler(IOrchestrationRepository orchestrationRepository)
//	{
//		_orchestrationRepository = orchestrationRepository ?? throw new ArgumentNullException(nameof(orchestrationRepository));
//	}

//	public async Task<IResult> HandleAsync(OrchestrationEvent @event, OrchestrationEventHandlerContext handlerContext, CancellationToken cancellationToken = default)
//	{
//		@event.Id = handlerContext.MessageId;

//		var result = new ResultBuilder();
//		var traceInfo = TraceInfo.Create(handlerContext.TraceInfo);

//		var saveResult = await _orchestrationRepository.EnqueueAsync(@event, traceInfo, cancellationToken).ConfigureAwait(false);
//		result.MergeHasError(saveResult);
//		return result.Build();
//	}

//	public void Dispose()
//	{
//	}
//}



using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Orchestrations.Model;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.EventHandlers;

public static class OrchestrationEventHandler
{
	public static async Task<MessageHandlerResult> HandleMessageAsync(IQueuedMessage<OrchestrationEvent> message, IMessageHandlerContext context, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder();
		var traceInfo = TraceInfo.Create(message.TraceInfo);

		if (message.Message == null)
			return context.MessageHandlerResultFactory.FromResult(
				result.WithInvalidOperationException(traceInfo, $"{nameof(message)}.{nameof(message.Message)} == null"));

		var @event = message.Message;
		@event.Id = message.MessageId;

		var orchestrationRepository = context.ServiceProvider?.GetRequiredService<IOrchestrationRepository>();
		if (orchestrationRepository == null)
			return context.MessageHandlerResultFactory.FromResult(
				result.WithInvalidOperationException(traceInfo, $"{nameof(orchestrationRepository)} == null"));

		var saveResult = await orchestrationRepository.SaveNewEventAsync(@event, traceInfo, context.TransactionContext, cancellationToken).ConfigureAwait(false);
		result.MergeHasError(saveResult);

		//var executionPointerFactory = context.ServiceProvider?.GetRequiredService<IExecutionPointerFactory>();
		//if (executionPointerFactory == null)
		//	return context.MessageHandlerResultFactory.FromResult(
		//		result.WithInvalidOperationException(traceInfo, $"{nameof(executionPointerFactory)} == null"));

		//var executionPointer = executionPointerFactory.BuildNextPointer(
		//	orchestrationInstance.OrchestrationDefinition,
		//	pointer,
		//	step.IdNextStep.Value);

		//if (executionPointer != null)
		//	await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, executionPointer).ConfigureAwait(false);


		var orchestrationInstances = await orchestrationRepository.GetOrchestrationInstancesAsync(@event.OrchestrationKey, context.ServiceProvider!, context.ServiceBusOptions.HostInfo, context.TransactionContext, default).ConfigureAwait(false);
		if (orchestrationInstances == null)
			return context.MessageHandlerResultFactory.FromResult(result.Build());

		foreach (var instance in orchestrationInstances)
			if (instance.Status == OrchestrationStatus.Running || instance.Status == OrchestrationStatus.Executing)
				await instance.StartOrchestrationWorkerAsync().ConfigureAwait(false);

		return context.MessageHandlerResultFactory.FromResult(result.Build());
	}
}