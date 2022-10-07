using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public interface IMessageBus
{
	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult> SendAsync(
		IRequestMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult> SendAsync(
		IRequestMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult> SendAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<Guid>> SendWithMessageIdAsync(
		IRequestMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<Guid>> SendWithMessageIdAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<Guid>> SendWithMessageIdAsync(
		IRequestMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<Guid>> SendWithMessageIdAsync(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<TResponse>> SendAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<ISendResponse<TResponse>>> SendWithMessageIdAsync<TResponse>(
		IRequestMessage<TResponse> message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<ISendResponse<TResponse>>> SendWithMessageIdAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<ISendResponse<TResponse>>> SendWithMessageIdAsync<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<ISendResponse<TResponse>>> SendWithMessageIdAsync<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult Send(
		IRequestMessage message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult Send(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	IResult Send(
		IRequestMessage message,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	IResult Send(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<Guid> SendWithMessageId(
		IRequestMessage message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<Guid> SendWithMessageId(
		IRequestMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	IResult<Guid> SendWithMessageId(
		IRequestMessage message,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	IResult<Guid> SendWithMessageId(
		IRequestMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	IResult<TResponse> Send<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<ISendResponse<TResponse>> SendWithMessageId<TResponse>(
		IRequestMessage<TResponse> message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	IResult<ISendResponse<TResponse>> SendWithMessageId<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder> optionsBuilder,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="traceInfo"></param>
	IResult<ISendResponse<TResponse>> SendWithMessageId<TResponse>(
		IRequestMessage<TResponse> message,
		ITraceInfo traceInfo);

	/// <summary>
	/// Sends a request message.
	/// </summary>
	/// <param name="message">The request message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	IResult<ISendResponse<TResponse>> SendWithMessageId<TResponse>(
		IRequestMessage<TResponse> message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo);
}
