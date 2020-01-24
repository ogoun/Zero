using System.Threading.Tasks;

namespace ZeroLevel.Services.Async.Internal
{
	internal static class TaskExtensions
	{
		public static async Task<T> WithYield<T>(this Task<T> task)
		{
			var result = await task.ConfigureAwait(false);
			await Task.Yield();
			return result;
		}
	}
}
