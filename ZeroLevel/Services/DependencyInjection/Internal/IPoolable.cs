namespace ZeroLevel.Patterns.DependencyInjection
{
    public interface IPoolable<T>
    {
        T Instance { get; }
        void Cleanup();
        void Release();
    }
}
