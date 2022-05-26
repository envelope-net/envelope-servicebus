using Envelope.ServiceBus.Messages;
using System.Text;

namespace Envelope.ServiceBus.Serialization;

public class BytesMessageBody : IMessageBody
{
	private readonly Encoding _encoding;
	private readonly byte[] _bytes;
	private string? _string;

	public long? Length => _bytes.Length;

	public BytesMessageBody(byte[]? bytes)
	{
		_bytes = bytes ?? Array.Empty<byte>();
		_encoding = Encoding.UTF8;
	}

	public BytesMessageBody(byte[]? bytes, Encoding encoding)
	{
		_bytes = bytes ?? Array.Empty<byte>();
		_encoding = encoding ?? Encoding.UTF8;
	}

	/// <inheritdoc/>
	public Stream? GetStream()
		=> new MemoryStream(_bytes);

	/// <inheritdoc/>
	public byte[]? GetBytes()
		=> _bytes;

	/// <inheritdoc/>
	public string? GetString()
		=> _string ??= _encoding.GetString(_bytes);
}
