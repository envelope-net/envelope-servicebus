using Envelope.Text;
using Envelope.Validation;
using System.Text;

namespace Envelope.ServiceBus.Exchange.Routing;

public class ExchangeRouter : IExhcangeRouter, IValidable
{
	/// <inheritdoc/>
	public string ExchangeName { get; set; }

	/// <inheritdoc/>
	public ExchangeType ExchangeType { get; set; }

	/// <inheritdoc/>
	public Dictionary<string, string> Bindings { get; } //Dictionary<TargetQueueName, RouteName>

	/// <inheritdoc/>
	public Dictionary<string, object>? Headers { get; set; } //Dictionary<Key, Value>

	/// <inheritdoc/>
	public HeadersMatch HeadersMatch { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ExchangeRouter()
	{
		Bindings = new Dictionary<string, string>();
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public bool MatcheHeaders(IEnumerable<KeyValuePair<string, object>>? headers)
	{
		if (headers == null || !headers.Any() || Headers == null || Headers.Count == 0)
			return false;

		if (HeadersMatch == HeadersMatch.All)
		{
			var matchedCount = 0;
			foreach (var kvp in headers)
			{
				if (!Headers.TryGetValue(kvp.Key, out var value))
					return false;

				if (kvp.Value != value)
					return false;

				matchedCount++;
			}

			return matchedCount == Headers.Count;
		}
		else
		{
			foreach (var kvp in headers)
				if (Headers.TryGetValue(kvp.Key, out var value) && kvp.Value == value)
					return true;

			return false;
		}
	}

	public StringBuilder? Validate(string? propertyPrefix = null, StringBuilder? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (string.IsNullOrWhiteSpace(ExchangeName))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(ExchangeName))} == null");
		}

		if (Bindings.Count == 0)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(Bindings))} == null");
		}

		if (ExchangeType == ExchangeType.Headers && (Headers == null || Headers.Count == 0))
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new StringBuilder();

			parentErrorBuffer.AppendLine($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(Headers))} == null");
		}

		return parentErrorBuffer;
	}
}
