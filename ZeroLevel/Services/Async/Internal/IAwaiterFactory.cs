namespace ZeroLevel.Services.Async.Internal
{
	internal interface IAwaiterFactory<T>
	{
		IAwaiter<T> CreateAwaiter();
	}
}
