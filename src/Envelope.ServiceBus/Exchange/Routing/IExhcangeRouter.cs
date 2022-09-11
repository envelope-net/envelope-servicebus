using Envelope.Validation;

namespace Envelope.ServiceBus.Exchange.Routing;

public interface IExhcangeRouter : IValidable
{
	string ExchangeName { get; set; }

	ExchangeType ExchangeType { get; set; }

	/// <summary>
	/// Bounded target queues
	/// </summary>
	Dictionary<string, string> Bindings { get; } //Dictionary<TargetQueueName, RouteName>

	/// <summary>
	/// For <see cref="ExchangeType.Headers"/> exchange type
	/// </summary>
	HeadersMatch HeadersMatch { get; set; }

	/// <summary>
	/// For <see cref="ExchangeType.Headers"/> exchange type
	/// </summary>
	Dictionary<string, object>? Headers { get; set; } //Dictionary<Key, Value>

	bool MatcheHeaders(IEnumerable<KeyValuePair<string, object>>? headers);
}
