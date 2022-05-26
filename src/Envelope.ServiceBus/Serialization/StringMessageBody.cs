using Envelope.ServiceBus.Messages;
using System.Text;

namespace Envelope.ServiceBus.Serialization;

public class StringMessageBody : IMessageBody
{
	private readonly Encoding _encoding;
	private readonly string? _text;
	private byte[]? _bytes;

	public long? Length => _text?.Length;

	public StringMessageBody(string text)
	{
		_text = text;
		_encoding = Encoding.UTF8;
	}

	public StringMessageBody(string text, Encoding encoding)
	{
		_text = text;
		_encoding = encoding ?? Encoding.UTF8;
	}

	/// <inheritdoc/>
	public Stream? GetStream()
		=> new MemoryStream(GetBytes()!);

	/// <inheritdoc/>
	public byte[]? GetBytes()
		=> _bytes ??= string.IsNullOrEmpty(_text)
			? Array.Empty<byte>()
			: _encoding.GetBytes(_text);

	/// <inheritdoc/>
	public string? GetString()
		=> _text;
}
