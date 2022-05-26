using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Envelope.ServiceBus.Messages;

public class MessageHeaders : IMessageHeaders, IEnumerable<MessageHeader>
{
	private readonly IDictionary<string, object> _headers;

	public MessageHeaders()
	{
		_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public MessageHeaders(IEnumerable<KeyValuePair<string, object>>? headers)
	{
		if (headers == null)
		{
			_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			_headers = new Dictionary<string, object>(headers, StringComparer.OrdinalIgnoreCase);
		}
	}

	[return: NotNullIfNotNull("headers")]
	public static MessageHeaders? Create(IEnumerable<KeyValuePair<string, object>>? headers)
	{
		if (headers == null)
			return null;

		return new MessageHeaders(headers);
	}

	public void Set(string key, string? value)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		if (value == null)
			_headers.Remove(key);
		else
			_headers[key] = value;
	}

	public void Set(string key, object? value, bool overwrite = true)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		if (overwrite)
		{
			if (value == null)
				_headers.Remove(key);
			else
				_headers[key] = value;
		}
		else if (value != null)
			_headers.TryAdd(key, value);
	}

	public bool TryGetHeader(string key, [MaybeNullWhen(false)] out object value)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		return _headers.TryGetValue(key, out value);
	}

	public IEnumerable<KeyValuePair<string, object>> GetAll()
	{
		return _headers;
	}

	public T? Get<T>(string key, T? defaultValue)
		where T : class
	{
		if (_headers.TryGetValue(key, out var value) && value is T tValue)
			return tValue;

		return defaultValue;
	}

	public T? Get<T>(string key, T? defaultValue)
		where T : struct
	{
		if (_headers.TryGetValue(key, out var value) && value is T tValue)
			return tValue;

		return defaultValue;
	}

	public IEnumerator<MessageHeader> GetEnumerator()
	{
		return _headers.Select(x => new MessageHeader(x.Key, x.Value)).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
