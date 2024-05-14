using Messenger;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos;
using System.Text;

namespace CactusFrontEnd.Cosmos.utils
{

	public class Utils
	{
		//private Container container;
		//public Utils(CosmosClient client)
		//{
		//	container = client.GetContainer("cactus-messenger", "cactus-messenger");
		//}

		public static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
		{
			List<T> result = [];
			await foreach (var item in source)
			{
				result.Add(item);
			}
			return result;
		}
		internal static string GetStringSha256Hash(string text)
		{
			if (String.IsNullOrEmpty(text))
				return String.Empty;

			using (var sha = new System.Security.Cryptography.SHA256Managed())
			{
				byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
				byte[] hash = sha.ComputeHash(textData);
				return BitConverter.ToString(hash).Replace("-", String.Empty);
			}
		}

		public static async IAsyncEnumerable<T> ExecuteQuery<T>(IQueryable<T> query)
		{
			FeedIterator<T> iterator = query
				.ToFeedIterator();

			while (iterator.HasMoreResults)
			{
				FeedResponse<T> page = await iterator.ReadNextAsync();
				foreach (var item in page)
				{
					yield return item;
				}
			}
		}
	}
}
