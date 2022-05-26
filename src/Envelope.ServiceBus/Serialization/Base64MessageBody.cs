using Envelope.ServiceBus.Messages;

namespace Envelope.ServiceBus.Serialization;

public class Base64MessageBody : IMessageBody
{
	private readonly string? _text;
	private byte[]? _bytes;

	public long? Length => _text?.Length;

	public Base64MessageBody(string text)
	{
		_text = text;
	}

	/// <inheritdoc/>
	public Stream? GetStream()
		=> new MemoryStream(GetBytes()!);

	/// <inheritdoc/>
	public byte[]? GetBytes()
		=> _bytes ??= string.IsNullOrEmpty(_text)
			? Array.Empty<byte>()
			: Convert.FromBase64String(_text);

	/// <inheritdoc/>
	public string? GetString()
		=> _text;
}