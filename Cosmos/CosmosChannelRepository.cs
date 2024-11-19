using Messenger;
using Microsoft.Azure.Cosmos;

namespace CactusFrontEnd.Cosmos;

public class CosmosChannelRepository : CosmosRepositoryBase<Channel>
{
	public CosmosChannelRepository(CosmosClient client) : base(client, "channel") { }
}