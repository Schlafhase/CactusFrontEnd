using CactusFrontEnd.Cosmos.utils;
using CactusFrontEnd.Exceptions;
using Messenger;
using MessengerInterfaces;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace CactusFrontEnd.Cosmos
{
	public abstract class CosmosRepositoryBase<T> : IRepository<T> where T : class, ICosmosObject
	{
		private readonly CosmosClient client;
		private readonly Container container;
		private readonly PartitionKey partitionKey;
		private readonly string type;
		public CosmosRepositoryBase(CosmosClient client, string type)
		{
			this.client = client;
			this.container = client.GetContainer("cactus-messenger", "cactus-messenger");
			this.partitionKey = new("id");
			this.type = type;
		}

		public async Task CreateNew(T entity)
		{
			try
			{
				var response = await container.CreateItemAsync<T>(entity, new PartitionKey(entity.Id.ToString()));
				if (response.StatusCode is HttpStatusCode.Conflict)
				{
					throw new StatusCodeException(HttpStatusCode.Conflict);
				}
			}
			catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.Conflict)
			{
				throw new StatusCodeException(HttpStatusCode.Conflict);
			}
			catch (Exception e)
			{
				throw;
			}
		}

		public async Task<List<T>> GetAll()
		{
			var q = this.GetQueryable();
			return await this.ToListAsync(q);
		}

		public async Task<T?> GetById(Guid id)
		{
			try
			{
				var q = this.GetQueryable()
					.Where(item => item.Id == id);
				List<T> result = await this.ToListAsync(q);
				//ItemResponse<T> result = await container.ReadItemAsync<T>(id.ToString(), new PartitionKey(id.ToString()));
				if (result.Count != 1)
				{
					return null;
				}
				return result.First();
			}
			catch (CosmosException e) when (e.StatusCode is HttpStatusCode.NotFound)
			{
				return null;
			}
		}

		public IQueryable<T> GetQueryable()
		{
			return container.GetItemLinqQueryable<T>()
				.Where(item => item.Type == this.type);
		}

		public async Task Replace(Guid id, T entity)
		{
			await container.ReplaceItemAsync<T>(entity, id.ToString(), partitionKey);
		}

		public async Task<List<T>> ToListAsync(IQueryable<T> query)
		{
			IAsyncEnumerable<T> response = Utils.ExecuteQuery<T>(query);
			List<T> result = await Utils.ToListAsync<T>(response);
			return result;
		}
	}
}
