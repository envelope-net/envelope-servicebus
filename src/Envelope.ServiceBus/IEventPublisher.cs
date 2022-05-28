using Envelope.ServiceBus.MessageHandlers;
using Envelope.ServiceBus.Messages;
using Envelope.ServiceBus.Messages.Options;
using Envelope.Services;
using Envelope.Trace;
using System.Runtime.CompilerServices;

namespace Envelope.ServiceBus;

public interface IEventPublisher
{
	/// <summary>
	/// Publishes an event
	/// </summary>
	/// <param name="event"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	/// <returns>List of created event IDs or warning if no <see cref="MessageHandlerContext"/> was created</returns>
	Task<IResult<List<Guid>>> PublishEventAsync(
		IEvent @event,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Publishes an event
	/// </summary>
	/// <param name="event"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="cancellationToken"></param>
	/// <param name="memberName">Allows you to obtain the method or property name of the caller to the method.</param>
	/// <param name="sourceFilePath">Allows you to obtain the full path of the source file that contains the caller. This is the file path at the time of compile.</param>
	/// <param name="sourceLineNumber">Allows you to obtain the line number in the source file at which the method is called.</param>
	/// <returns>List of created event IDs or warning if no <see cref="MessageHandlerContext"/> was created</returns>
	Task<IResult<List<Guid>>> PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder> optionsBuilder,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0);

	/// <summary>
	/// Publishes an event
	/// </summary>
	/// <param name="event"></param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>List of created event IDs or warning if no <see cref="MessageHandlerContext"/> was created</returns>
	Task<IResult<List<Guid>>> PublishEventAsync(
		IEvent @event,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Publishes an event
	/// </summary>
	/// <param name="event"></param>
	/// <param name="optionsBuilder">Configure the message sending options</param>
	/// <param name="traceInfo"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>List of created event IDs or warning if no <see cref="MessageHandlerContext"/> was created</returns>
	Task<IResult<List<Guid>>> PublishEventAsync(
		IEvent @event,
		Action<MessageOptionsBuilder>? optionsBuilder,
		ITraceInfo traceInfo,
		CancellationToken cancellationToken = default);
}
