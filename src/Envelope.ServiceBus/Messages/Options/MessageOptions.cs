using Envelope.ServiceBus.ErrorHandling;
using Envelope.Text;
using Envelope.Transactions;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Messages.Options;

public class MessageOptions : IMessageOptions, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	/// <inheritdoc/>
	public ITransactionContext TransactionContext { get; set; }

	/// <inheritdoc/>
	public string ExchangeName { get; set; }

	/// <inheritdoc/>
	public string ContentType { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	/// <inheritdoc/>
	public Encoding? ContentEncoding { get; set; }

	/// <inheritdoc/>
	public bool DisabledMessagePersistence { get; set; }

	/// <inheritdoc/>
	public Guid? IdSession { get; set; }

	/// <inheritdoc/>
	public string? RoutingKey { get; set; }

	public bool IsAsynchronousInvocation { get; set; }

	/// <inheritdoc/>
	public IErrorHandlingController? ErrorHandling { get; set; }

	public IMessageHeaders? Headers { get; set; }

	/// <inheritdoc/>
	public TimeSpan? Timeout { get; set; }

	/// <inheritdoc/>
	public bool IsCompressContent { get; set; }

	/// <inheritdoc/>
	public bool IsEncryptContent { get; set; }

	/// <inheritdoc/>
	public int Priority { get; set; }

	/// <inheritdoc/>
	public bool DisableFaultQueue { get; set; }

	public bool? ThrowNoHandlerException { get; set; }

	/// <inheritdoc/>
	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(ExchangeName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeName))} == null"));
		}

		if (string.IsNullOrWhiteSpace(ContentType))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ContentType))} == null"));
		}

		return parentErrorBuffer;
	}
}
