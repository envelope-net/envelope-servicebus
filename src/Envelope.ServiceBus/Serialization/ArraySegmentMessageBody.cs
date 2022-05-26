using Envelope.ServiceBus.Messages;
using System.Text;

namespace Envelope.ServiceBus.Serialization;

public class ArraySegmentMessageBody : IMessageBody
{
	private readonly Encoding _encoding;
	private readonly ArraySegment<byte> _bytes;

	public long? Length => _bytes.Count;

	public ArraySegmentMessageBody(ArraySegment<byte> bytes)
	{
		_bytes = bytes;
		_encoding = Encoding.UTF8;
	}

	public ArraySegmentMessageBody(ArraySegment<byte> bytes, Encoding encoding)
	{
		_bytes = bytes;
		_encoding = encoding ?? Encoding.UTF8;
	}

	/// <inheritdoc/>
	public Stream? GetStream()
		=> _bytes.Array != null
			? new MemoryStream(_bytes.Array, _bytes.Offset, _bytes.Count, false)
			: null;

	/// <inheritdoc/>
	public byte[]? GetBytes()
		=> _bytes.Array != null
			? _bytes.ToArray()
			: null;

	/// <inheritdoc/>
	public string? GetString()
		=> _bytes.Array != null
			? _encoding.GetString(_bytes.Array, _bytes.Offset, _bytes.Count)
			: null;
}
