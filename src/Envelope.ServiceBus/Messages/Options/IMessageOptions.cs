using Envelope.ServiceBus.ErrorHandling;
using Envelope.Transactions;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Messages.Options;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessageOptions : IValidable
{
	ITransactionController TransactionController { get; set; }

	string ExchangeName { get; set; }

	/// <summary>
	/// If true, the message will never be saved to storage
	/// </summary>
	bool DisabledMessagePersistence { get; set; }

	bool? ThrowNoHandlerException { get; set; }

	/// <summary>
	/// Id of the original request that launched the session. Used for tracing messages
	/// </summary>
	Guid? IdSession { get; set; }

	string ContentType { get; set; }

	Encoding? ContentEncoding { get; set; }

	string? RoutingKey { get; set; }

	bool IsAsynchronousInvocation { get; set; }

	IErrorHandlingController? ErrorHandling { get; set; }

	IMessageHeaders? Headers { get; set; }

	/// <summary>
	/// The timespan after which the Send request will be cancelled if no response arrives.
	/// </summary>
	TimeSpan? Timeout { get; set; }

	bool IsCompressContent { get; set; }

	bool IsEncryptContent { get; set; }

	int Priority { get; set; }

	bool DisableFaultQueue { get; set; }
}
