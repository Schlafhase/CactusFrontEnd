using CactusFrontEnd.Utils;
using Messenger;
using Microsoft.Azure.Cosmos;

namespace CactusFrontEnd.Cosmos
{
	public class CosmosAccountRepository: CosmosRepositoryBase<Account>
	{
		public CosmosAccountRepository(CosmosClient client) : base(client, "account") { }
    }
}
