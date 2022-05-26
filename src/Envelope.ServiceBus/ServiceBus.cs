using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

internal class ServiceBus : IServiceBus
{
	private readonly IServiceBusOptions _options;

	public ServiceBus(IServiceBusOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
	}

	public Task<IResult<List<Guid>, Guid>> PublishAsync<TMessage>(
		TMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage
		=> PublishAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<List<Guid>, Guid>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage
		=> PublishAsync(message, optionsBuilder, TraceInfo<Guid>.Create(null, _options.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber), cancellationToken);

	public Task<IResult<List<Guid>, Guid>> PublishAsync<TMessage>(
		TMessage message,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
		=> PublishAsync(message, (Action<MessageOptionsBuilder>?)null, traceInfo, cancellationToken);

	public Task<IResult<List<Guid>, Guid>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
	{
		var builder = MessageOptionsBuilder.GetDefaultBuilder<TMessage>();
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return PublishAsync(message, options, traceInfo, cancellationToken);
	}

	protected async Task<IResult<List<Guid>, Guid>> PublishAsync<TMessage>(
		TMessage message,
		IMessageOptions options,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder<List<Guid>, Guid>();

		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(TraceInfo<Guid>.Create(_options.HostInfo.HostName), nameof(traceInfo));

		try
		{
			var dispatchResult = await DispatchAsync(traceInfo, message, options, cancellationToken);
			if (result.MergeAllWithDataHasError(dispatchResult))
				return result.Build();

			return result.WithData(dispatchResult.Data).Build();
		}
		catch (Exception exHost)
		{
			var errorMessage =
				await _options.HostLogger.LogErrorAsync(
					TraceInfo<Guid>.Create(traceInfo),
					_options.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exHost),
					$"{nameof(PublishAsync)}<{typeof(TMessage).FullName}> error",
					null,
					cancellationToken);

			result.WithError(errorMessage);
			return result.Build();
		}
	}

	private async Task<IResult<List<Guid>, Guid>> DispatchAsync<TMessage>(ITraceInfo<Guid> traceInfo, TMessage? message, IMessageOptions options, CancellationToken cancellationToken)
		where TMessage : class, IMessage
	{
		traceInfo = TraceInfo<Guid>.Create(traceInfo);
		var result = new ResultBuilder<List<Guid>, Guid>();

		var exchange = _options.ExchangeProvider.GetExchange<TMessage>(options.ExchangeName);
		if (exchange == null)
		{
			var errorMessage = _options.HostLogger.LogError(
				traceInfo,
				_options.HostInfo,
				HostStatus.Unchanged,
				x => x.InternalMessage($"queue == null | {nameof(options.ExchangeName)} == {options.ExchangeName} | MessageType = {message?.GetType().FullName}"),
				$"{nameof(DispatchAsync)}<{nameof(TMessage)}> queue == null",
				null);
			result.WithError(errorMessage);

			if (!options.DisableFaultQueue)
			{
				try
				{
					var context = _options.ExchangeProvider.CreateFaultQueueContext(traceInfo, options);
					await _options.ExchangeProvider.FaultQueue.EnqueueAsync(message, context, cancellationToken);
				}
				catch (Exception ex)
				{
					var faultEnqueueErrorMessage = _options.HostLogger.LogError(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x
							.ExceptionInfo(ex)
							.Detail($"queue == null | {nameof(options.ExchangeName)} == {options.ExchangeName} | MessageType = {message?.GetType().FullName} >> {nameof(_options.ExchangeProvider.FaultQueue)}.{nameof(_options.ExchangeProvider.FaultQueue.EnqueueAsync)}"),
						$"{nameof(DispatchAsync)}<{nameof(TMessage)}> queue == null >> {nameof(_options.ExchangeProvider.FaultQueue)}",
						null);
					result.WithError(faultEnqueueErrorMessage);
				}
			}

			return result.Build();
		}

		try
		{
			var contextResult = _options.ExchangeProvider.CreateExchangeEnqueueContext(traceInfo, options, exchange.ExchangeType);
			if (result.MergeHasError(contextResult))
				return result.Build();

			var enqueueResult = await exchange.EnqueueAsync(message, contextResult.Data!, cancellationToken);
			if (result.MergeHasError(enqueueResult))
				return result.Build();

			return result.WithData(enqueueResult.Data).Build();
		}
		catch (Exception ex)
		{
			var errorMessage = _options.HostLogger.LogError(
				traceInfo,
				_options.HostInfo,
				HostStatus.Unchanged,
				x => x
					.ExceptionInfo(ex)
					.Detail($"{nameof(options.ExchangeName)} == {options.ExchangeName} | MessageType = '{message?.GetType().FullName}'"),
				$"{nameof(DispatchAsync)}<{nameof(TMessage)}>",
				null);
			result.WithError(errorMessage);

			if (!options.DisableFaultQueue)
			{
				try
				{
					var context = _options.ExchangeProvider.CreateFaultQueueContext(traceInfo, options);
					await _options.ExchangeProvider.FaultQueue.EnqueueAsync(message, context, cancellationToken);
				}
				catch (Exception faultEx)
				{
					var faultEnqueueErrorMessage = _options.HostLogger.LogError(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x
							.ExceptionInfo(faultEx)
							.Detail($"{nameof(options.ExchangeName)} == {options.ExchangeName} | MessageType = '{message?.GetType().FullName}' >> {nameof(_options.ExchangeProvider.FaultQueue)}.{nameof(_options.ExchangeProvider.FaultQueue.EnqueueAsync)}"),
						$"{nameof(DispatchAsync)}<{nameof(TMessage)}> >> {nameof(_options.ExchangeProvider.FaultQueue)}",
						null);
					result.WithError(faultEnqueueErrorMessage);
				}
			}

			return result.Build();
		}
	}

	#region IEventPublisher

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo<Guid>.Create(null, _options.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken);
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo<Guid>.Create(null, _options.HostInfo.HostName, null, memberName, sourceFilePath, sourceLineNumber);

		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken);
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken);
	}

	async Task<IResult<List<Guid>, Guid>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>, Guid>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken);
	}

	#endregion IEventPublisher
}
