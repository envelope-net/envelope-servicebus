namespace Envelope.ServiceBus.Messages.Resolvers;

internal class MessageType : IMessageType, Envelope.Serializer.IDictionaryObject
{
	public string Name { get; set; }
	public string CrlType { get; set; }
	public MessageMetaType MessageMetaType { get; set; }
	public IMessageType? ResponseMessageType { get; set; }


	public MessageType(string name, string crlType, MessageMetaType messageMetaType)
	{
		Name = name;
		CrlType = crlType;
		MessageMetaType = messageMetaType;
	}

	public IDictionary<string, object?> ToDictionary(Envelope.Serializer.ISerializer? serializer = null)
	{
		var dict = new Dictionary<string, object?>
		{
			{ nameof(Name), Name },
			{ nameof(CrlType), CrlType },
			{ nameof(MessageMetaType), MessageMetaType },
		};

		if (ResponseMessageType != null)
		{
			dict.Add($"{nameof(ResponseMessageType)}.{nameof(ResponseMessageType.Name)}", ResponseMessageType.Name);
			dict.Add($"{nameof(ResponseMessageType)}.{nameof(ResponseMessageType.CrlType)}", ResponseMessageType.CrlType);
			dict.Add($"{nameof(ResponseMessageType)}.{nameof(ResponseMessageType.MessageMetaType)}", ResponseMessageType.MessageMetaType);
		}

		return dict;
	}
}
