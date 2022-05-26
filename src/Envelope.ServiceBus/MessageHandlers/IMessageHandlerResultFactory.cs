using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus.MessageHandlers;

public interface IMessageHandlerResultFactory
{
	MessageHandlerResult FromResult(
		IResult<Guid> result,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	MessageHandlerResult FromResult(
		IResult<Guid> result,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	MessageHandlerResult Completed();

	MessageHandlerResult Error(IResult<Guid> errorResult, TimeSpan? retryInterval = null);

	MessageHandlerResult Suspended(IResult<Guid> errorResult);

	MessageHandlerResult Deferred(TimeSpan retryInterval);

	MessageHandlerResult DeliveredInternal();

	MessageHandlerResult AbortedInternal(IResult<Guid> errorResult);



	MessageHandlerResult<TResponse> FromResult<TResponse>(
		IResult<TResponse, Guid> result,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	MessageHandlerResult<TResponse> FromResult<TResponse>(
		IResult<TResponse, Guid> result,
		ITraceInfo<Guid> traceInfo,
		TimeSpan? errorRetryInterval = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	MessageHandlerResult<TResponse> Completed<TResponse>(TResponse result);

	MessageHandlerResult<TResponse> Error<TResponse>(IResult<Guid> errorResult, TimeSpan? retryInterval = null);

	MessageHandlerResult<TResponse> Suspended<TResponse>(IResult<Guid> errorResult);

	MessageHandlerResult<TResponse> Deferred<TResponse>(TimeSpan retryInterval);

	internal MessageHandlerResult<TResponse> Delivered<TResponse>();

	internal MessageHandlerResult<TResponse> Aborted<TResponse>(IResult<Guid> errorResult);
}
