using CactusFrontEnd.Components;
using CactusFrontEnd.Exceptions;
using CactusFrontEnd.Utils;
using Messenger;
using MessengerInterfaces;
using Microsoft.AspNetCore.Components;

namespace CactusFrontEnd.Cosmos
{
	public class MessengerService : IMessengerService
	{
		public Action<ChannelDTO_Output> OnMessage { get; set; }

		private readonly AsyncLocker asyncLocker;
		private IRepository<Account> accountRepo;
		private IRepository<Channel> channelRepo;
		private IRepository<Message> messageRepo;
		private EventService eventService;

		public MessengerService(IRepository<Account> accountRepo, IRepository<Channel> channelRepo, IRepository<Message> messageRepo, EventService eventService)
		{
			asyncLocker = new AsyncLocker();
			this.accountRepo = accountRepo;
			this.channelRepo = channelRepo;
			this.messageRepo = messageRepo;
			this.eventService = eventService;
		}

		public async Task InitializeAsync()
		{
			try
			{
				await channelRepo.CreateNew(new Channel([CactusConstants.EveryoneId], CactusConstants.GlobalChannelId, "Global Channel"));
			}
			catch { }
		}

		//Message related methods
		public async Task<MessageDTO_Output> GetMessage(Guid Id, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getMessage(Id, userId);
		}

		private async Task<MessageDTO_Output> getMessage(Guid Id, Guid userId)
		{
			Message msg = await messageRepo.GetById(Id);
			Account author = await getAccount(msg.AuthorId);
			ChannelDTO_Output channel = await getChannel(msg.ChannelId, userId);
			Account user = await getAccount(userId);

			if (channel.Users.Contains(user.Id) || channel.Users.Contains(CactusConstants.EveryoneId) || user.IsAdmin)
			{
				try
				{
					MessageDTO_Output msgDTO = new(msg, author.UserName, author.IsAdmin);
					return msgDTO;
				}
				catch (KeyNotFoundException)
				{
					throw new KeyNotFoundException($"Unable to find message with Id {Id}");
				}
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to view messages in this channel.");
			}
		}

		public async Task PostMessage(Message message, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			ChannelDTO_Output channel = await getChannel(message.ChannelId, userId);
			Account user = await getAccount(userId);
			if (channel.Users.Contains(user.Id) || channel.Users.Contains(CactusConstants.EveryoneId) || user.IsAdmin)
			{
				await messageRepo.CreateNew(message);
				OnMessage?.Invoke(channel);
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to post in this channel.");
			}
		}

		public async Task DeleteMessage(Guid id, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			MessageDTO_Output msg = await getMessage(id, userId);
			Account user = await getAccount(userId);
			if (msg.AuthorId == userId || user.IsAdmin)
			{
				ChannelDTO_Output channel = await getChannel(msg.ChannelId, userId);
				//delete message
				await messageRepo.DeleteItem(id);
				OnMessage?.Invoke(channel);
			}
		}

		public async Task DeleteAllMessages()
		{
			using IDisposable _ = await asyncLocker.Enter();
			await messageRepo.DeleteItemsWithFilter(item => true);
		}

		public async Task<MessageDTO_Output[]> GetAllMessages(Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			List<Message> messages;
			Account user = await getAccount(userId);

			messages = await messageRepo.GetAll();

			if (user.IsAdmin)
			{
				return await convertMessagesToDtos(messages);
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to view all Messages (Admin only request).");
			}
		}

		public async Task<MessageDTO_Output[]> GetAllMessagesInChannel(Guid channelId, Guid userId)
		{
			List<Message> messages;
			Account user;
			ChannelDTO_Output channel;
			using IDisposable _ = await asyncLocker.Enter();
			channel = await getChannel(channelId, userId);
			user = await getAccount(userId);
			IQueryable<Message> query = messageRepo.GetQueryable()
				.Where(msg => msg.ChannelId == channelId);


			if (channel.Users.Contains(user.Id) || channel.Users.Contains(CactusConstants.EveryoneId) || user.IsAdmin)
			{
				messages = await messageRepo.ToListAsync(query);
				return await convertMessagesToDtos(messages);
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to view messages in this channel.");
			}
		}

		//Channel related methods
		public async Task<Guid> CreateChannel(HashSet<Guid> userIds, Guid userId, string name)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (userIds.Contains(user.Id) || user.IsAdmin)
			{
				Guid channelId = Guid.NewGuid();
				await channelRepo.CreateNew(new Channel(userIds, channelId, name));
				eventService.ChannelsHaveChanged();
				return channelId;
			}
			else
			{
				throw new UnauthorizedAccessException("User must be member of the channel (Except the user has administrative permissions)");
			}
		}

		public async Task DeleteChannel(Guid channelId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			await deleteChannel(channelId);
			eventService.ChannelsHaveChanged();
		}

		private async Task deleteChannel(Guid channelId)
		{
			//delete all messages in channel
			await messageRepo.DeleteItemsWithFilter(msg => msg.ChannelId == channelId);
			//delete channel
			await channelRepo.DeleteItem(channelId);
		}

		public async Task RemoveUserFromChannel(Guid channelId, Guid accountId, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			await removeUserFromChannel(channelId, accountId, userId);
		}

		private async Task removeUserFromChannel(Guid channelId, Guid accountId, Guid userId)
		{
			ChannelDTO_Output channelDTO = await getChannel(channelId, userId);
			channelDTO.Users.Remove(accountId);
			if (channelDTO.Users.Count == 0)
			{
				await deleteChannel(channelId);
			}
			else
			{
				Channel channel = new(channelDTO.Users, channelId, channelDTO.Name);
				await channelRepo.Replace(channelId, channel);
			}
			eventService.ChannelsHaveChanged();
		}

		public async Task<ChannelDTO_Output> GetChannel(Guid channelId, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getChannel(channelId, userId);
		}

		public async Task<ChannelDTO_Output[]> GetChannelsWithUser(Guid accountId, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			IQueryable<Channel> query = channelRepo.GetQueryable()
				.Where(channel => channel.Users.Contains(accountId));
			List<Channel> channels = await channelRepo.ToListAsync(query);
			return await convertChannelsToDtos(channels);
		}

		public async Task<ChannelDTO_Output[]> GetAllChannels()
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getAllChannels();
		}

		private async Task<ChannelDTO_Output[]> getAllChannels()
		{
			IQueryable<Channel> query = channelRepo.GetQueryable();
		    List<Channel> channels = await channelRepo.ToListAsync(query);
			return await convertChannelsToDtos(channels);
		}

		private async Task<ChannelDTO_Output> getChannel(Guid channelId, Guid userId)
		{
			Account user = await getAccount(userId);
			Channel channel;
			try
			{
				channel = await channelRepo.GetById(channelId);
			}
			catch (KeyNotFoundException)
			{
				throw new KeyNotFoundException($"Unable to find channel with Id {channelId}");
			}
			if (channel == null)
			{
				throw new KeyNotFoundException($"Unable to find channel with Id {channelId}");
			}
			if (channel.Users.Contains(user.Id) || channel.Users.Contains(CactusConstants.EveryoneId) || user.IsAdmin)
			{
				ChannelDTO_Output channelDTO = await createChannelDTO_OutputFromChannel(channel);
				return channelDTO;
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to see the requested channel.");
			}
		}

		public async Task AddUserToChannel(Guid Id, Guid channelId, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			Channel channel;
			try
			{
				channel = await channelRepo.GetById(channelId);
			}
			catch (KeyNotFoundException)
			{
				throw new KeyNotFoundException($"Unable to find channel with Id {channelId}");
			}
			if (channel.Users.Contains(user.Id) || user.IsAdmin)
			{
				channel.Users.Add(Id);
				await channelRepo.Replace(channelId, channel);
				eventService.ChannelsHaveChanged();
			}
			else
			{
				throw new UnauthorizedAccessException("Can only add users to channels you are in (Except for Admin accounts)");
			}
		}

		//Account related methods
		public async Task<bool> LoginAccount(Guid id, string password)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(id);
			string passwordHash = Utils.Utils.GetStringSha256Hash(password + user.Id.ToString());
			return passwordHash == user.PasswordHash;
		}

		public async Task DeleteAccount(Guid id)
		{
			if (id == CactusConstants.AdminId || id == CactusConstants.DeletedId)
			{
				throw new ArgumentException("This account can't be deleted.");
			}
			using IDisposable _ = await asyncLocker.Enter();
			//remove user from all channels
			IQueryable<Message> messageQuery = messageRepo.GetQueryable()
				.Where(msg => msg.AuthorId == id);
			List<Message> messages = await messageRepo.ToListAsync(messageQuery);
			await Task.WhenAll(messages
				.Select(msg =>
				{
					Message newMessage = new Message(msg.Id, msg.Content, msg.DateTime, CactusConstants.DeletedId, msg.ChannelId);
					return messageRepo.Replace(msg.Id, newMessage);
				}));
			IQueryable<Guid> channelQuery = channelRepo.GetQueryable()
				.Where(channel => channel.Users.Contains(id))
				.Select(channel => channel.Id);

			List<Guid> channelIds = await channelRepo.ToListAsync(channelQuery);
			await Task.WhenAll(channelIds
				.Select(channelId =>
				{
					return removeUserFromChannel(channelId, id, id);
				}));
			eventService.ChannelsHaveChanged();
			//change all authorids from user to deleted CactusConstants.DeletedId

			//delete account
			await accountRepo.DeleteItem(id);
		}

		public async Task<Guid> CreateAccount(string username, string password)
		{
			return await CreateAccount(username, password, null);
		}

		public async Task<Guid> CreateAccount(string username, string password, string? email)
		{

			using IDisposable _ = await asyncLocker.Enter();
			try
			{
				await getAccountByUsername(username);
			}
			catch (KeyNotFoundException)
			{
				Guid userId = Guid.NewGuid();
				string passwordHash = Utils.Utils.GetStringSha256Hash(password + userId.ToString());
				await accountRepo.CreateNew(new Account(username, passwordHash, userId, email));
				return userId;
			}
			throw new UsernameExistsException();
		}

		public async Task EditAccountAdmin(Guid id, bool giveAdmin, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (user.IsAdmin)
			{
				Account target = await getAccount(id);
				target.IsAdmin = giveAdmin;
				await accountRepo.Replace(id, target);
			}
			else
			{
				throw new UnauthorizedAccessException("No permission (Only admins can change the admin status of other users)");
			}
		}

		public async Task EditAccountLock(Guid id, bool newState, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (user.IsAdmin)
			{
				Account target = await getAccount(id);
				target.Locked = newState;
				await accountRepo.Replace(id, target);
			}
			else
			{
				throw new UnauthorizedAccessException("Acces denied (Only admins can lock/unlock accounts)");
			}
		}

		public async Task EditAccountEmail(Guid id, string email, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (user.IsAdmin || user.Id == id)
			{
				Account target = await getAccount(id);
				target.Email = email;
				await accountRepo.Replace(id, target);
			}
			else
			{
				throw new UnauthorizedAccessException($"Acces denied (Only admins or the owner of the account with ID {id} can change the email adress of this account.");
			}
		}

		public async Task ChangePW(Guid Id, Guid userId, string newPW)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (user.IsAdmin || user.Id == Id)
			{
				Account target = await getAccount(Id);
				string passwordHash = Utils.Utils.GetStringSha256Hash(newPW + Id.ToString());
				target.PasswordHash = passwordHash;
				await accountRepo.Replace(Id, target);
			}
			else
			{
				throw new UnauthorizedAccessException("Acces denied (Only the owner of the account/admins can edit the password of an account)");
			}
		}

		public async Task<Account> GetAccount(Guid Id)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getAccount(Id);
		}

		public async Task<Account> GetAccountByUsername(string username)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getAccountByUsername(username);
		}

		public async Task<Account[]> GetAllAccounts()
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getAllAccounts();
		}

		private async Task<Account> getAccountByUsername(string username)
		{
			IQueryable<Account> q = accountRepo.GetQueryable()
							.Where(item => item.UserName == username);
			List<Account> result = await accountRepo.ToListAsync(q);
			if (result.Count == 1)
			{
				return result[0];
			}
			else
			{
				throw new KeyNotFoundException("User not found");
			}
		}

		private async Task<Account> getAccount(Guid Id)
		{
			Account? acc = await accountRepo.GetById(Id);
			if (acc is null)
			{
				throw new KeyNotFoundException($"Unable to find Account with Id {Id}");
			};
			return acc;
		}

		private async Task<Account[]> getAllAccounts()
		{
			IQueryable<Account> q = accountRepo.GetQueryable();
			List<Account> result = await accountRepo.ToListAsync(q);
			return result.ToArray();
		}

		//other methods

		private async Task<MessageDTO_Output[]> convertMessagesToDtos(List<Message> messages)
		{
			return await Task.WhenAll(messages
							.Select(async (msg) =>
							{
								try
								{
									Account author = await getAccount(msg.AuthorId);
									return new MessageDTO_Output(msg, author.UserName, author.IsAdmin);
								}
								catch (Exception)
								{
									return null!;
								}
							})
							.Where(x => x != null)
							.ToArray());
		}

		private async Task<ChannelDTO_Output[]> convertChannelsToDtos(List<Channel> channels)
		{
			return await Task.WhenAll(channels
							.Select(async (chnl) =>
							{
								string[] userNames = await Task.WhenAll(chnl.Users
									.Select(async userId =>
									{
										Account user = await getAccount(userId);
										return user.UserName;
									}));
								return new ChannelDTO_Output(chnl, userNames.ToHashSet());
							})
							.Where(x => x != null)
							.ToArray());
		}

		private async Task<ChannelDTO_Output> createChannelDTO_OutputFromChannel(Channel channel)
		{
			string[] UserNames = await Task.WhenAll(channel.Users
				.Select(async userId =>
				{
					if (userId == CactusConstants.EveryoneId) return "Everyone";
					Account user = await getAccount(userId);
					return user.UserName;
				}));
			return new ChannelDTO_Output(channel, UserNames.ToHashSet());
		}
	}
}