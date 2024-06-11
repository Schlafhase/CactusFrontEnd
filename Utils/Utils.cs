using Messenger;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CactusFrontEnd.Utils
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
			await foreach (T? item in source)
			{
				result.Add(item);
			}
			return result;
		}
		internal static string GetStringSha256Hash(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			using (System.Security.Cryptography.SHA256Managed sha = new System.Security.Cryptography.SHA256Managed())
			{
				byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
				byte[] hash = sha.ComputeHash(textData);
				return BitConverter.ToString(hash).Replace("-", string.Empty);
			}
		}

		public static async IAsyncEnumerable<T> ExecuteQuery<T>(IQueryable<T> query)
		{
			FeedIterator<T> iterator = query
				.ToFeedIterator();

			while (iterator.HasMoreResults)
			{
				FeedResponse<T> page = await iterator.ReadNextAsync();
				foreach (T? item in page)
				{
					yield return item;
				}
			}
		}

		public static async Task DeleteQuery<T>(IQueryable<T> query)
		{
			FeedIterator<T> iterator = query
				.ToFeedIterator();

			while (iterator.HasMoreResults)
			{
				FeedResponse<T> page = await iterator.ReadNextAsync();
				foreach (T? item in page)
				{
					
				}
			}
		}

		public static string Relativize(DateTime date1, DateTime date2)
		{
			TimeSpan diff = date2 - date1;

			if (diff.Days > 7)
			{
				return date1.ToString();
			}
			else if (diff.Days >= 1)
			{
				return $"{diff.Days} " + (diff.Days != 1 ? "days" : "day") + $" ago at {date1.Hour}:{date1.Minute}";
			}
            else if (diff.Hours >= 1)
            {
				return $"{diff.Hours} " + (diff.Hours != 1 ? "hours" : "hour") + " ago";
            }
			else
			{
				return $"{diff.Minutes} " + (diff.Minutes != 1 ? "minutes" : "minute") + " ago";
			}
        }
	}
}
