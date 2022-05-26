using Envelope.Extensions;
using Envelope.ServiceBus.Messages;
using System.Text;

namespace Envelope.ServiceBus.Serialization;

public class StreamMessageBody : IMessageBody
{
	private readonly Encoding _encoding;
	private readonly Stream? _stream;
	private byte[]? _bytes;
	private string? _string;

	public long? Length => _stream?.Length;

	public StreamMessageBody(Stream stream)
	{
		_stream = stream;
		_encoding = Encoding.UTF8;
	}

	public StreamMessageBody(Stream stream, Encoding encoding)
	{
		_stream = stream;
		_encoding = encoding ?? Encoding.UTF8;
	}

	/// <inheritdoc/>
	public Stream? GetStream()
	{
		if (_stream != null && _stream.CanSeek)
			_stream.Seek(0, SeekOrigin.Begin);

		return _stream;
	}

	/// <inheritdoc/>
	public byte[]? GetBytes()
		=> _bytes ??= _stream != null
			? _stream.ToArray()
			: Array.Empty<byte>();

	/// <inheritdoc/>
	public string? GetString()
		=> _string ??= _encoding.GetString(GetBytes()!);
}
