using CactusFrontEnd.Cosmos.utils;
using CactusFrontEnd.Exceptions;
using Messenger;
using MessengerInterfaces;

namespace CactusFrontEnd.Cosmos
{
	public class MessengerService : IMessengerService
	{
		public Action<ChannelDTO_Output> OnMessage { get; set; }

		private readonly AsyncLocker asyncLocker;
		private IRepository<Account> accountRepo;
		private IRepository<Channel> channelRepo;
		private IRepository<Message> messageRepo;

		public MessengerService(IRepository<Account> accountRepo, IRepository<Channel> channelRepo, IRepository<Message> messageRepo)
		{
			asyncLocker = new AsyncLocker();
			this.accountRepo = accountRepo;
			this.channelRepo = channelRepo;
			this.messageRepo = messageRepo;

		}

		//Message related methods
		public async Task<MessageDTO_Output> GetMessage(Guid Id, Guid userId)
		{
			Message msg;
			using IDisposable _ = await asyncLocker.Enter();
			msg = await messageRepo.GetById(Id);
			Account author = await getAccount(msg.AuthorId);
			ChannelDTO_Output channel = await getChannel(msg.ChannelId, userId);
			Account user = await getAccount(userId);

			if (channel.Users.Contains(user.Id) || user.IsAdmin)
			{
				try
				{
					MessageDTO_Output msgDTO = new(msg, author.UserName);
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
			if (channel.Users.Contains(user.Id) || user.IsAdmin)
			{
				await messageRepo.CreateNew(message);
				OnMessage.Invoke(channel);
			}
			else
			{
				throw new UnauthorizedAccessException("User has no permission to post in this channel.");
			}
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
			var query = messageRepo.GetQueryable()
				.Where(msg => msg.ChannelId == channelId);


			if (channel.Users.Contains(user.Id) || user.IsAdmin)
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
		public async Task<Guid> CreateChannel(HashSet<Guid> userIds, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (userIds.Contains(user.Id) || user.IsAdmin)
			{
				Guid channelId = Guid.NewGuid();
				await channelRepo.CreateNew(new Channel(userIds, channelId));
				return channelId;
			}
			else
			{
				throw new UnauthorizedAccessException("User must be member of the channel (Except the user has administrative permissions)");
			}
		}

		public async Task<ChannelDTO_Output> GetChannel(Guid channelId, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getChannel(channelId, userId);
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
			if (channel.Users.Contains(user.Id) || user.IsAdmin)
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
			string passwordHash = Utils.GetStringSha256Hash(password + user.Id.ToString());
			return passwordHash == user.PasswordHash;
		}

		public async Task<Guid> CreateAccount(string username, string password)
		{

			using IDisposable _ = await asyncLocker.Enter();
			try
			{
				await getAccountByUsername(username);
			}
			catch (KeyNotFoundException)
			{
				Guid userId = Guid.NewGuid();
				string passwordHash = Utils.GetStringSha256Hash(password + userId.ToString());
				await accountRepo.CreateNew(new Account(username, passwordHash, userId));
				return userId;
			}
			throw new UsernameExistsException();
		}

		public async Task EditAccountAdmin(Guid Id, bool giveAdmin, Guid userId)
		{
			using IDisposable _ = await asyncLocker.Enter();
			Account user = await getAccount(userId);
			if (user.IsAdmin)
			{
				user.IsAdmin = giveAdmin;
				await accountRepo.Replace(Id, user);
			}
			else
			{
				throw new UnauthorizedAccessException("No permissions (Only admins can change the admin status of other users)");
			}
		}

		public async Task<Account> GetAccount(Guid Id)
		{
			using IDisposable _ = await asyncLocker.Enter();
			return await getAccount(Id);
		}

		public async Task<Account> GetAccountByUsername(string username)
		{
			using var _ = await asyncLocker.Enter();
			return await getAccountByUsername(username);
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
			try
			{
				return await accountRepo.GetById(Id);
			}
			catch (KeyNotFoundException)
			{
				throw new Exception($"Unable to find Account with Id {Id}");
			};
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
									return new MessageDTO_Output(msg, author.UserName);
								}
								catch (Exception)
								{
									return null!;
								}
							})
							.Where(x => x != null)
							.ToArray());
		}

		private async Task<ChannelDTO_Output> createChannelDTO_OutputFromChannel(Channel channel)
		{
			string[] UserNames = await Task.WhenAll(channel.Users
				.Select(async userId =>
				{
					Account user = await getAccount(userId);
					return user.UserName;
				}));
			return new ChannelDTO_Output(channel, UserNames.ToHashSet());
		}

		public async Task InitializeAsync()
		{
			try
			{
				Guid andrewId = await this.CreateAccount("Linus", "$chlafHase2009");
			}
			catch { }
		}
	}
}