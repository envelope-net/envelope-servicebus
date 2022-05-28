using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.Messages;
using Envelope.Trace;
using System.Text;

namespace Envelope.ServiceBus.Exchange.Internal;

internal class ExchangeEnqueueContext : IExchangeEnqueueContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string PublisherId { get; set; }

	public ITraceInfo TraceInfo { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public bool DisabledMessagePersistence { get; set; }

	public Guid? IdSession { get; set; }

	public string? ContentType { get; set; }

	public Encoding? ContentEncoding { get; set; }

	public string? RoutingKey { get; set; }

	public IErrorHandlingController? ErrorHandling { get; set; }

	public IMessageHeaders? Headers { get; set; }

	public bool IsAsynchronousInvocation { get; set; }

	public TimeSpan? Timeout { get; set; }

	public bool IsCompressContent { get; set; }

	public bool IsEncryptContent { get; set; }

	public int Priority { get; set; }

	public bool DisableFaultQueue { get; set; }
}
