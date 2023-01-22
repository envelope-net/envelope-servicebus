namespace Envelope.ServiceBus.Messages;

#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ISendResponse<TResponse>
{
	Guid RequestId { get; }
	Guid ResponseId { get; }
	TResponse? Response { get; }
}
