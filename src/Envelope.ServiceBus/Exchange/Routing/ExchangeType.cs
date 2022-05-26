namespace Envelope.ServiceBus.Exchange.Routing;

public enum ExchangeType
{
	/// <summary>
	/// Baesd on RoutingKey, exact match
	/// </summary>
	Direct = 0,

	/// <summary>
	/// All target queues bounded to
	/// </summary>
	FanOut = 1,

	/// <summary>
	/// Baesd on RoutingKey, regex pattern
	/// </summary>
	Topic = 2,

	/// <summary>
	/// Basded on Message headers
	/// </summary>
	Headers = 3
}
