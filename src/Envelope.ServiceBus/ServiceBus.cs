using Envelope.ServiceBus.Configuration;
using Envelope.ServiceBus.Exchange;
using Envelope.ServiceBus.Hosts;
using Envelope.ServiceBus.Jobs;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Services.Transactions;
using Envelope.Trace;
using Envelope.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

internal class ServiceBus : IServiceBus
{
	private readonly IServiceBusOptions _options;

	public ServiceBus(IServiceProvider serviceProvider, IServiceBusConfiguration configuration)
	{
		var options = ServiceBusOptionsFactory.Create(serviceProvider, configuration);
		_options = options;
	}

	public ServiceBus(IServiceBusOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
	}

	public void Initialize(ITraceInfo traceInfo, CancellationToken cancellationToken = default)
	{
		traceInfo = TraceInfo.Create(traceInfo);

		foreach (var queue in _options.QueueProvider.GetAllQueues())
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await queue.OnMessageAsync(traceInfo, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					await _options.HostLogger.LogErrorAsync(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(ex),
						$"{nameof(Initialize)} >> {nameof(queue.OnMessageAsync)}: QUEUE = {queue.QueueName}",
						null,
						cancellationToken: default).ConfigureAwait(false);
				}

			},
			cancellationToken: default);
		}

		foreach (var exchange in _options.ExchangeProvider.GetAllExchanges())
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await exchange.OnMessageAsync(traceInfo, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					await _options.HostLogger.LogErrorAsync(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(ex),
						$"{nameof(Initialize)} >> {nameof(exchange.OnMessageAsync)}: EXCHANGE = {exchange.QueueName}",
						null,
						cancellationToken: default).ConfigureAwait(false);
				}

			},
			cancellationToken: default);
		}

		var jobController = _options.ServiceProvider.GetService<IJobController>();
		if (jobController != null)
			_ = Task.Run(async () =>
			{
				try
				{
					await jobController.StartAllAsync(traceInfo).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					await _options.HostLogger.LogErrorAsync(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(ex),
						$"{nameof(Initialize)} >> {nameof(jobController.StartAllAsync)}: Jobs",
						null,
						cancellationToken: default).ConfigureAwait(false);
				}

			},
			cancellationToken: default);
	}

	public Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage
		=> PublishAsync(message, null!, cancellationToken, memberName, sourceFilePath, sourceLineNumber);

	public Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage
		=> PublishAsync(
			message,
			optionsBuilder,
			TraceInfo.Create(
				_options.ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
				null, //_options.HostInfo.HostName,
				null,
				memberName,
				sourceFilePath,
				sourceLineNumber),
			cancellationToken);

	public Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
		=> PublishAsync(message, (Action<MessageOptionsBuilder>?)null, traceInfo, cancellationToken);

	public Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
	{
		var builder = MessageOptionsBuilder.GetDefaultBuilder<TMessage>();
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return PublishAsync(message, options, traceInfo, cancellationToken);
	}

	protected async Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		IMessageOptions options,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage
	{
		var result = new ResultBuilder<List<Guid>>();

		if (options == null)
			return result.WithArgumentNullException(traceInfo, nameof(options));
		if (traceInfo == null)
			return result.WithArgumentNullException(
				TraceInfo.Create(
					_options.ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo
					//_options.HostInfo.HostName
					),
				nameof(traceInfo));

		try
		{
			var dispatchResult = await DispatchAsync(traceInfo, message, options, cancellationToken).ConfigureAwait(false);
			if (result.MergeAllWithDataHasError(dispatchResult))
				return result.Build();

			return result.WithData(dispatchResult.Data).Build();
		}
		catch (Exception exHost)
		{
			var errorMessage =
				await _options.HostLogger.LogErrorAsync(
					TraceInfo.Create(traceInfo),
					_options.HostInfo,
					HostStatus.Unchanged,
					x => x.ExceptionInfo(exHost),
					$"{nameof(PublishAsync)}<{typeof(TMessage).FullName}> error",
					null,
					cancellationToken).ConfigureAwait(false);

			result.WithError(errorMessage);
			return result.Build();
		}
	}

	protected virtual ITransactionController CreateTransactionController()
		=> _options.ServiceProvider.GetRequiredService<ITransactionCoordinator>().TransactionController;

	private async Task<IResult<List<Guid>>> DispatchAsync<TMessage>(ITraceInfo traceInfo, TMessage? message, IMessageOptions options, CancellationToken cancellationToken)
		where TMessage : class, IMessage
	{
		traceInfo = TraceInfo.Create(traceInfo);
		var result = new ResultBuilder<List<Guid>>();

		var isLocalTransactionCoordinator = false;

		var transactionController = options.TransactionController;
		if (transactionController == null)
		{
			transactionController = CreateTransactionController();
			isLocalTransactionCoordinator = true;
		}

		IExchange<TMessage>? exchange = null;
		IExchangeEnqueueContext? exchangeContext = null;

		var executeResult =
			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionController,
				async (traceInfo, transactionController, cancellationToken) =>
				{
					exchange = _options.ExchangeProvider.GetExchange<TMessage>(options.ExchangeName);
					if (exchange == null)
					{
						var errorMessage = await _options.HostLogger.LogErrorAsync(
							traceInfo,
							_options.HostInfo,
							HostStatus.Unchanged,
							x => x.InternalMessage($"exchange == null | {nameof(options.ExchangeName)} == {options.ExchangeName} | MessageType = {typeof(TMessage).FullName}"),
							$"{nameof(DispatchAsync)}<{nameof(TMessage)}> exchange == null",
							null,
							cancellationToken: default).ConfigureAwait(false);

						result.WithError(errorMessage);

						await WriteToFaultQueueAsync<TMessage>(traceInfo, message, options, cancellationToken).ConfigureAwait(false);

						return result.Build();
					}

					var contextResult = _options.ExchangeProvider.CreateExchangeEnqueueContext(traceInfo, options, exchange.ExchangeType, _options.ServiceBusMode);
					if (result.MergeHasError(contextResult))
						return result.Build();

					exchangeContext = contextResult.Data!;

					var exchangeEnqueueResult = await exchange.EnqueueAsync(message, exchangeContext, transactionController, cancellationToken).ConfigureAwait(false);
					result.MergeHasError(exchangeEnqueueResult);

					if (exchangeEnqueueResult.HasError)
					{
						transactionController.ScheduleRollback();
					}
					else
					{
						if (isLocalTransactionCoordinator)
							transactionController.ScheduleCommit();
					}

					if (result.HasError())
						return result.Build();

					return result.WithData(exchangeEnqueueResult.Data).Build();
				},
				$"{nameof(DispatchAsync)}<{typeof(TMessage).FullName}> | {nameof(options.ExchangeName)} == {options.ExchangeName} - {nameof(IExchange<TMessage>.EnqueueAsync)}",
				async (traceInfo, exception, detail) =>
				{
					var errorMessage =
						await _options.HostLogger.LogErrorAsync(
							traceInfo,
							_options.HostInfo,
							HostStatus.Unchanged,
							x => x.ExceptionInfo(exception).Detail(detail),
							detail,
							null,
							cancellationToken: default).ConfigureAwait(false);

					await WriteToFaultQueueAsync<TMessage>(traceInfo, message, options, cancellationToken).ConfigureAwait(false);

					return errorMessage;
				},
				null,
				isLocalTransactionCoordinator,
				cancellationToken).ConfigureAwait(false);

		if (result.MergeHasError(executeResult))
			return result.Build();

		if (exchange != null && _options.ServiceBusMode == ServiceBusMode.PublishSubscribe && exchangeContext?.CallExchangeOnMessage == true)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					var ti = TraceInfo.Create(traceInfo);
					await exchange.OnMessageAsync(ti, cancellationToken).ConfigureAwait(false);
					if (exchangeContext.OnMessageQueue != null)
						await exchangeContext.OnMessageQueue.OnMessageAsync(ti, cancellationToken: default).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					await _options.HostLogger.LogErrorAsync(
						TraceInfo.Create(traceInfo),
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(ex),
						$"{nameof(DispatchAsync)} >> {nameof(exchange.OnMessageAsync)}",
						null,
						cancellationToken: default).ConfigureAwait(false);
				}

			},
			cancellationToken: default);
		}

		return result.Build();
	}

	private async Task WriteToFaultQueueAsync<TMessage>(ITraceInfo traceInfo, IMessage? message, IMessageOptions options, CancellationToken cancellationToken)
	{
		if (!options.DisableFaultQueue)
		{
			traceInfo = TraceInfo.Create(traceInfo);
			var result = new ResultBuilder();
			var transactionController = CreateTransactionController();

			await ServiceTransactionInterceptor.ExecuteActionAsync(
				false,
				traceInfo,
				transactionController,
				async (traceInfo, transactionController, cancellationToken) =>
				{
					var context = _options.ExchangeProvider.CreateFaultQueueContext(traceInfo, options);
					var enqueueResult = await _options.ExchangeProvider.FaultQueue.EnqueueAsync(message, context, transactionController, cancellationToken).ConfigureAwait(false);
					result.MergeAllHasError(enqueueResult);

					if (enqueueResult.HasError)
					{
						transactionController.ScheduleRollback();
					}
					else
					{
						transactionController.ScheduleCommit();
					}

					return result.Build();
				},
				$"{nameof(WriteToFaultQueueAsync)}<{typeof(TMessage).FullName}> | {nameof(options.ExchangeName)} == {options.ExchangeName}",
				async (traceInfo, exception, detail) =>
				{
					var errorMessage = await _options.HostLogger.LogErrorAsync(
						traceInfo,
						_options.HostInfo,
						HostStatus.Unchanged,
						x => x.ExceptionInfo(exception).Detail(detail),
						detail,
						cancellationToken: default).ConfigureAwait(false);

					return errorMessage;
				},
				null,
				true,
				cancellationToken).ConfigureAwait(false);
		}
	}

	#region IEventPublisher

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo.Create(
			_options.ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
			null, //_options.HostInfo.HostName,
			null,
			memberName,
			sourceFilePath,
			sourceLineNumber);

		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken,
		string memberName,
		string sourceFilePath,
		int sourceLineNumber)
	{
		var traceInfo = TraceInfo.Create(
			_options.ServiceProvider.GetRequiredService<IApplicationContext>().TraceInfo,
			null, //_options.HostInfo.HostName,
			null,
			memberName,
			sourceFilePath,
			sourceLineNumber);

		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	async Task<IResult<List<Guid>>> IEventPublisher.PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken)
	{
		var result = new ResultBuilder<List<Guid>>();

		if (@event == null)
			return result.WithArgumentNullException(traceInfo, nameof(@event));

		var builder = MessageOptionsBuilder.GetDefaultBuilder(@event.GetType());
		optionsBuilder?.Invoke(builder);
		var options = builder.Build(true);

		return await PublishAsync(@event, options, traceInfo, cancellationToken).ConfigureAwait(false);
	}

	#endregion IEventPublisher
}
