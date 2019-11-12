using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Collections
{
    public interface IEverythingStorage
    {
        IEnumerable<string> Keys<T>();

        #region Generic
        bool TryAdd<T>(string key, T value);

        bool ContainsKey<T>(string key);

        bool TryRemove<T>(string key);

        void Add<T>(string key, T value);

        void AddOrUpdate<T>(string key, T value);

        void Remove<T>(string key);

        T Get<T>(string key);
        #endregion

        bool TryAdd(Type type, string key, object value);

        bool ContainsKey(Type type, string key);

        bool TryRemove(Type type, string key);

        void Add(Type type, string key, object value);

        void AddOrUpdate(Type type, string key, object value);

        void Remove(Type type, string key);

        object Get(Type type, string key);
    }
}