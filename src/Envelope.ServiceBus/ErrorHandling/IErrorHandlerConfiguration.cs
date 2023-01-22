using Envelope.ServiceBus.ErrorHandling;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IErrorHandlerConfiguration : IValidable
{
	IReadOnlyDictionary<int, TimeSpan> IterationRetryTable { get; set; } //Dictionary<IterationCount, TimeSpan>

	TimeSpan? DefaultRetryInterval { get; set; }

	int? MaxRetryCount { get; set; }

	IErrorHandlingController BuildErrorHandlingController();
}
