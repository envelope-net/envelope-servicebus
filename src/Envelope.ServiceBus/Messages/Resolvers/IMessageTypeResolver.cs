namespace Envelope.ServiceBus.Messages.Resolvers;

public interface IMessageTypeResolver
{
	string ToName(Type type);
	Type ToType(string name);
}
