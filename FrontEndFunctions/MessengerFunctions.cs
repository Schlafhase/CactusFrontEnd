using Messenger;
using MessengerInterfaces;

namespace CactusFrontEnd.FrontEndFunctions;

public class MessengerFunctions
{
	public static async Task SendMessage(string content, Guid channelId, Guid userId, IMessageService messageService)
	{
		MessageDTO_Input msg = new(content);
		await messageService.PostMessage(msg.ToMessage(userId, channelId), userId);
	}

	public static async Task<MessageDTO_Output[]> GetMessages(Guid            channelId,
	                                                          Guid            userId,
	                                                          IMessageService messageService)
	{
		return await messageService.GetAllMessagesInChannel(channelId, userId);
	}

	public static Guid TryParseGuid(string guid)
	{
		Guid Id;

		try
		{
			Id = Guid.Parse(guid);
		}
		catch
		{
			Id = CactusConstants.DeletedId;
		}

		return Id;
	}
}