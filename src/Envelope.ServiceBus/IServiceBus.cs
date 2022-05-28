using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.ServiceBus.Model;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public interface IServiceBus : IEventPublisher
{
	/// <summary>
	/// Publishes a message and do not wait for the response from target handler/s.
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage;

	/// <summary>
	/// Publishes a message and do not wait for the response from target handler/s.
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0)
		where TMessage : class, IMessage;

	/// <summary>
	/// Publishes a message and do not wait for the response from target handler/s.
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage;

	/// <summary>
	/// Publishes a message and do not wait for the response from target handler/s.
	/// </summary>
	/// <param name="message">The message</param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	Task<IResult<List<Guid>>> PublishAsync<TMessage>(
		TMessage message,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default)
		where TMessage : class, IMessage;
}