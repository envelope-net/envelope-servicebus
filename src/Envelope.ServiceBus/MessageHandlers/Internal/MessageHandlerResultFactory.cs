using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus.MessageHandlers.Internal;

internal class MessageHandlerResultFactory : IMessageHandlerResultFactory
{
	public MessageHandlerResult FromResult(
		IResult<Guid> result,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		if (result == null)
		{
			ITraceInfo<Guid> traceInfo = TraceInfo<Guid>.Create($"---{nameof(ServiceBus)}---", (Guid?)null, null, null, memberName, sourceFilePath, sourceLineNumber);
			var resultBuilder = new ResultBuilder<Guid>();
			result = resultBuilder.WithArgumentNullException(traceInfo, nameof(result));
		}

		if (result.HasError)
		{
			return Error(result, errorRetryInterval);
		}
		else
		{
			return Completed();
		}
	}

	public MessageHandlerResult FromResult(
		IResult<Guid> result,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		if (result == null)
		{
			if (traceInfo == null)
				traceInfo = TraceInfo<Guid>.Create($"---{nameof(ServiceBus)}---", (Guid?)null, null, null, memberName, sourceFilePath, sourceLineNumber);

			var resultBuilder = new ResultBuilder<Guid>();
			result = resultBuilder.WithArgumentNullException(traceInfo, nameof(result));
		}

		if (result.HasError)
		{
			return Error(result, errorRetryInterval);
		}
		else
		{
			return Completed();
		}
	}

	public MessageHandlerResult Completed()
	{
		return new MessageHandlerResult
		{
			Processed = true,
			MessageStatus = MessageStatus.Completed,
			ErrorResult = null,
			Retry = false,
			RetryInterval = null
		};
	}

	public MessageHandlerResult Error(IResult<Guid> errorResult, TimeSpan? retryInterval = null)
	{
		return new MessageHandlerResult
		{
			Processed = false,
			MessageStatus = MessageStatus.Error,
			ErrorResult = errorResult,
			Retry = true,
			RetryInterval = retryInterval
		};
	}

	public MessageHandlerResult Suspended(IResult<Guid> errorResult)
	{
		return new MessageHandlerResult
		{
			Processed = false,
			MessageStatus = MessageStatus.Suspended,
			ErrorResult = null,
			Retry = false,
			RetryInterval = null
		};
	}

	public MessageHandlerResult Deferred(TimeSpan retryInterval)
	{
		return new MessageHandlerResult
		{
			Processed = false,
			MessageStatus = MessageStatus.Deferred,
			ErrorResult = null,
			Retry = false,
			RetryInterval = retryInterval
		};
	}

	public MessageHandlerResult DeliveredInternal()
	{
		return new MessageHandlerResult
		{
			Processed = true,
			MessageStatus = MessageStatus.Delivered,
			ErrorResult = null,
			Retry = false,
			RetryInterval = null
		};
	}

	public MessageHandlerResult AbortedInternal(IResult<Guid> errorResult)
	{
		return new MessageHandlerResult
		{
			Processed = false,
			MessageStatus = MessageStatus.Aborted,
			ErrorResult = errorResult,
			Retry = false,
			RetryInterval = null
		};
	}






	public MessageHandlerResult<TResponse> FromResult<TResponse>(
		IResult<TResponse, Guid> result,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		if (result == null)
		{
			ITraceInfo<Guid> traceInfo = TraceInfo<Guid>.Create($"---{nameof(ServiceBus)}---", (Guid?)null, null, null, memberName, sourceFilePath, sourceLineNumber);
			var resultBuilder = new ResultBuilder<TResponse, Guid>();
			result = resultBuilder.WithArgumentNullException(traceInfo, nameof(result));
		}

		if (result.HasError)
		{
			return Error<TResponse>(result, errorRetryInterval);
		}
		else
		{
			return Completed(result.Data!);
		}
	}

	public MessageHandlerResult<TResponse> FromResult<TResponse>(
		IResult<TResponse, Guid> result,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
	{
		if (result == null)
		{
			if (traceInfo == null)
				traceInfo = TraceInfo<Guid>.Create($"---{nameof(ServiceBus)}---", (Guid?)null, null, null, memberName, sourceFilePath, sourceLineNumber);

			var resultBuilder = new ResultBuilder<TResponse, Guid>();
			result = resultBuilder.WithArgumentNullException(traceInfo, nameof(result));
		}

		if (result.HasError)
		{
			return Error<TResponse>(result, errorRetryInterval);
		}
		else
		{
			return Completed(result.Data!);
		}
	}

	public MessageHandlerResult<TResponse> Completed<TResponse>(TResponse result)
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = true,
			MessageStatus = MessageStatus.Completed,
			ErrorResult = null,
			Retry = false,
			RetryInterval = null,
			Result = result
		};
	}

	public MessageHandlerResult<TResponse> Error<TResponse>(IResult<Guid> errorResult, TimeSpan? retryInterval = null)
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = false,
			MessageStatus = MessageStatus.Error,
			ErrorResult = errorResult,
			Retry = true,
			RetryInterval = retryInterval,
			Result = default
		};
	}

	public MessageHandlerResult<TResponse> Suspended<TResponse>(IResult<Guid> errorResult)
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = false,
			MessageStatus = MessageStatus.Suspended,
			ErrorResult = errorResult,
			Retry = false,
			RetryInterval = null,
			Result = default
		};
	}

	public MessageHandlerResult<TResponse> Deferred<TResponse>(TimeSpan retryInterval)
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = false,
			MessageStatus = MessageStatus.Deferred,
			ErrorResult = null,
			Retry = false,
			RetryInterval = retryInterval,
			Result = default
		};
	}

	public MessageHandlerResult<TResponse> Delivered<TResponse>()
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = true,
			MessageStatus = MessageStatus.Delivered,
			ErrorResult = null,
			Retry = false,
			RetryInterval = null,
			Result = default
		};
	}

	public MessageHandlerResult<TResponse> Aborted<TResponse>(IResult<Guid> errorResult)
	{
		return new MessageHandlerResult<TResponse>
		{
			Processed = false,
			MessageStatus = MessageStatus.Aborted,
			ErrorResult = errorResult,
			Retry = false,
			RetryInterval = null,
			Result = default
		};
	}
}
