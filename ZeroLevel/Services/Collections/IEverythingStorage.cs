namespace ZeroLevel.Services.Collections
{
    public interface IEverythingStorage
    {
        bool TryAdd<T>(string key, T value);
        bool ContainsKey<T>(string key);
        bool TryRemove<T>(string key);
        void Add<T>(string key, T value);
        void AddOrUpdate<T>(string key, T value);
        void Remove<T>(string key);
        T Get<T>(string key);
    }
}
