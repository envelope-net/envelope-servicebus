using Envelope.ServiceBus.ErrorHandling;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public interface IErrorHandlerConfiguration : IValidable
{
	IReadOnlyDictionary<int, TimeSpan> IterationRetryTable { get; set; } //Dictionary<IterationCount, TimeSpan>

	TimeSpan? DefaultRetryInterval { get; set; }

	int? MaxRetryCount { get; set; }

	IErrorHandlingController BuildErrorHandlingController();
}
