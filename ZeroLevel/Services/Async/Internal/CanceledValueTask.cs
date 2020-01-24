using System.Threading.Tasks;

namespace ZeroLevel.Services.Async.Internal
{
	internal static class CanceledValueTask<T>
	{
		public static readonly ValueTask<T> Value = CreateCanceledTask();

		private static ValueTask<T> CreateCanceledTask()
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
			tcs.SetCanceled();
			return new ValueTask<T>(tcs.Task);
		}
	}
}
