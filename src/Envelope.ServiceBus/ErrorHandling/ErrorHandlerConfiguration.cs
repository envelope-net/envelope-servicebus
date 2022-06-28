using Envelope.Exceptions;
using Envelope.ServiceBus.ErrorHandling;
using Envelope.ServiceBus.ErrorHandling.Internal;
using Envelope.Text;
using Envelope.Validation;

namespace Envelope.ServiceBus.Configuration;

public class ErrorHandlerConfiguration : IErrorHandlerConfiguration, IValidable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	
	public Dictionary<int, TimeSpan> IterationRetryTable { get; set; } //Dictionary<IterationCount, TimeSpan>
	IReadOnlyDictionary<int, TimeSpan> IErrorHandlerConfiguration.IterationRetryTable { get => IterationRetryTable; set => IterationRetryTable = new Dictionary<int, TimeSpan>(value); }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public TimeSpan? DefaultRetryInterval { get; set; }

	public int? MaxRetryCount { get; set; }

	public List<IValidationMessage>? Validate(string? propertyPrefix = null, List<IValidationMessage>? parentErrorBuffer = null, Dictionary<string, object>? validationContext = null)
	{
		if (DefaultRetryInterval <= TimeSpan.Zero)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(DefaultRetryInterval))} <= Zero"));
		}

		if (MaxRetryCount < 0)
		{
			if (parentErrorBuffer == null)
				parentErrorBuffer = new List<IValidationMessage>();

			parentErrorBuffer.Add(ValidationMessageFactory.Error($"{StringHelper.ConcatIfNotNullOrEmpty(propertyPrefix, ".", nameof(MaxRetryCount))} < 0"));
		}

		return parentErrorBuffer;
	}

	public IErrorHandlingController BuildErrorHandlingController()
	{
		var error = Validate(nameof(ErrorHandlerConfiguration))?.ToString();
		if (!string.IsNullOrWhiteSpace(error))
			throw new ConfigurationException(error);

		var errorHandlingController = new ErrorHandlingController
		{
			IterationRetryTable = IterationRetryTable,
			DefaultRetryInterval = DefaultRetryInterval,
			MaxRetryCount = MaxRetryCount
		};

		return errorHandlingController;
	}
}
