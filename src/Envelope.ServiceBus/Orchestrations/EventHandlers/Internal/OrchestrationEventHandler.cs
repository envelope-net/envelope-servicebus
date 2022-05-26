﻿//using Envelope.ServiceBus.MessageHandlers;
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

//	public async Task<IResult<Guid>> HandleAsync(OrchestrationEvent @event, OrchestrationEventHandlerContext handlerContext, CancellationToken cancellationToken = default)
//	{
//		@event.Id = handlerContext.MessageId;

//		var result = new ResultBuilder<Guid>();
//		var traceInfo = TraceInfo<Guid>.Create(handlerContext.TraceInfo);

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
using Envelope.ServiceBus.Orchestrations.Persistence;
using Envelope.ServiceBus.Queues;
using Envelope.Services;
using Envelope.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Envelope.ServiceBus.Orchestrations.EventHandlers.Internal;

internal static class OrchestrationEventHandler
{
	public static async Task<MessageHandlerResult> HandleMessageAsync(IQueuedMessage<OrchestrationEvent> message, IMessageHandlerContext context, CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<Guid>();
		var traceInfo = TraceInfo<Guid>.Create(message.TraceInfo);

		if (message.Message == null)
			return context.MessageHandlerResultFactory.FromResult(
				result.WithInvalidOperationException(traceInfo, $"{nameof(message)}.{nameof(message.Message)} == null"));

		var @event = message.Message;
		@event.Id = message.MessageId;

		var orchestrationRepository = context.ServiceProvider?.GetRequiredService<IOrchestrationRepository>();
		if (orchestrationRepository == null)
			return context.MessageHandlerResultFactory.FromResult(
				result.WithInvalidOperationException(traceInfo, $"{nameof(orchestrationRepository)} == null"));

		var saveResult = await orchestrationRepository.SaveNewEventAsync(@event, traceInfo, cancellationToken).ConfigureAwait(false);
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
		//	await _orchestrationRepository.AddExecutionPointerAsync(orchestrationInstance.IdOrchestrationInstance, executionPointer);


		var orchestrationInstance = await orchestrationRepository.GetOrchestrationInstanceAsync(@event.OrchestrationKey, default);
		if (orchestrationInstance == null || (orchestrationInstance.Status != OrchestrationStatus.Running && orchestrationInstance.Status != OrchestrationStatus.Executing))
			return context.MessageHandlerResultFactory.FromResult(result.Build());

		await orchestrationInstance.StartOrchestrationWorkerAsync();

		return context.MessageHandlerResultFactory.FromResult(result.Build());
	}
}