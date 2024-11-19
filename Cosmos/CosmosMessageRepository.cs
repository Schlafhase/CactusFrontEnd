using Messenger;
using Microsoft.Azure.Cosmos;

namespace CactusFrontEnd.Cosmos;

public class CosmosMessageRepository : CosmosRepositoryBase<Message>
{
	public CosmosMessageRepository(CosmosClient client) : base(client, "message") { }
}