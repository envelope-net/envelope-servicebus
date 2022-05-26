namespace Envelope.ServiceBus.Messages.Resolvers;

/// <summary>
/// <see cref="IMessageTypeResolver"/> that uses the <see cref="Type.AssemblyQualifiedName"/> for converting type to string.
/// </summary>
public class AssemblyQualifiedNameTypeResolver : IMessageTypeResolver
{
	public string ToName(Type type)
		=> type?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(type));

	public Type ToType(string name)
		=> Type.GetType(name ?? throw new ArgumentNullException(nameof(name))) ?? throw new InvalidOperationException($"Name {name} cannot be resolved to any type.");
}
