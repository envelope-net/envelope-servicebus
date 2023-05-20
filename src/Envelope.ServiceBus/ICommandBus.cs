using Envelope.ServiceBus.Messages;
using Envelope.Services;
using Envelope.Trace;
using Envelope.Transactions;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public interface ICommandBus
{
	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult> SendAsync(
		ICommand command,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult> SendAsync(
		ICommand command,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult> SendAsync(
		ICommand command,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult> SendAsync(
		ICommand command,
		ITransactionController transactionController,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		ICommand<TResponse> command,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		ICommand<TResponse> command,
		ITransactionController transactionController,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		ICommand<TResponse> command,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a command and wait for the response from CommandHandler
	/// </summary>
	/// <param name="command"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		ICommand<TResponse> command,
		ITransactionController transactionController,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);
}
