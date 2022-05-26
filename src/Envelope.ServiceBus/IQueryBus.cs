using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public interface IQueryBus
{
	/// <summary>
	/// Sends a query and wait for the response from QueryHandler
	/// </summary>
	/// <param name="query"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IQuery<TResponse> query,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a query and wait for the response from QueryHandler
	/// </summary>
	/// <param name="query"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IQuery<TResponse> query,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a query and wait for the response from QueryHandler
	/// </summary>
	/// <param name="query"></param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IQuery<TResponse> query,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a query and wait for the response from QueryHandler
	/// </summary>
	/// <param name="query"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<ISendResponse<TResponse>, Guid>> SendAsync<TResponse>(
		IQuery<TResponse> query,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo<Guid> traceInfo,
		CancellationToken cancellationToken = default);
}
