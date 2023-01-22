namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface IMessageBody
{
	/// <summary>
	/// Return the message body as a stream
	/// </summary>
	/// <returns></returns>
	Stream? GetStream();

	/// <summary>
	/// Return the message body as a byte array
	/// </summary>
	/// <returns></returns>
	byte[]? GetBytes();

	/// <summary>
	/// Return the message body as a string
	/// </summary>
	/// <returns></returns>
	string? GetString();
}
