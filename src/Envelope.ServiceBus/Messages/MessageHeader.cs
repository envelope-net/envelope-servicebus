namespace Envelope.ServiceBus.Messages;

public readonly struct MessageHeader<T>
{
	public readonly string Key;
	public readonly T Value;

	public MessageHeader(string key, T value)
	{
		Key = key;
		Value = value;
	}

	public bool IsStringValue(out MessageHeader<string> result)
	{
		switch (this)
		{
			case MessageHeader<string> resultValue:
				result = resultValue;
				return true;
			default:
				return MessageHeader.IsValueStringValue(Key, Value, out result);
		}
	}

	public bool IsSimpleValue(out MessageHeader result)
	{
		return MessageHeader.IsValueSimpleValue(Key, Value, out result);
	}
}

public readonly struct MessageHeader
{
	public readonly string Key;
	public readonly object Value;

	public MessageHeader(string key, object value)
	{
		Key = key;
		Value = value;
	}

	public MessageHeader(KeyValuePair<string, object> pair)
	{
		Key = pair.Key;
		Value = pair.Value;
	}

	public bool IsStringValue(out MessageHeader<string> result)
	{
		return IsValueStringValue(Key, Value, out result);
	}

	public bool IsSimpleValue(out MessageHeader result)
	{
		return IsValueSimpleValue(Key, Value, out result);
	}

	public static implicit operator MessageHeader(MessageHeader<string> headerValue)
	{
		return new MessageHeader(headerValue.Key, headerValue.Value);
	}

	internal static bool IsValueStringValue(string key, object? value, out MessageHeader<string> result)
	{
		switch (value)
		{
			case null:
				result = default;
				return false;
			case string stringValue:
				result = new MessageHeader<string>(key, stringValue);
				return true;
			case bool boolValue when boolValue:
				result = new MessageHeader<string>(key, bool.TrueString);
				return true;
			case Uri uri:
				result = new MessageHeader<string>(key, uri.ToString());
				return true;
			case IFormattable formatValue when formatValue.GetType().IsValueType:
				result = new MessageHeader<string>(key, formatValue.ToString()!);
				return true;
			default:
				result = default;
				return false;
		}
	}

	internal static bool IsValueSimpleValue(string key, object? value, out MessageHeader result)
	{
		switch (value)
		{
			case null:
				result = default;
				return false;
			case string stringValue:
				result = new MessageHeader<string>(key, stringValue);
				return true;
			case bool boolValue when boolValue:
				result = new MessageHeader(key, true);
				return true;
			case Uri uri:
				result = new MessageHeader<string>(key, uri.ToString());
				return true;
			case IFormattable formatValue when formatValue.GetType().IsValueType:
				result = new MessageHeader(key, value);
				return true;
			default:
				result = default;
				return false;
		}
	}
}
