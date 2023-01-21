namespace Envelope.ServiceBus.DistributedCoordinator;

/// <summary>
/// v ramci releasovania rozdistribuuj === zosynchronizuj aj contextove data (ISyncData), ktore sa zmenili, pocas toho co bol aktivny lock
/// </summary>
#if NET6_0_OR_GREATER
[Envelope.Serializer.JsonPolymorphicConverter]
#endif
public interface ISyncData
{
	string Owner { get; set; }

	//TODO: do class-y, ktora bude implementovat tento interface, pridaj kontextove/projektove data pre synchronizaciu
}
