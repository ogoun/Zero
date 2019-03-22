using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Invokation;

namespace ZeroLevel.Services.Collections
{
    public class EverythingStorage :
        IEverythingStorage
    {
        private class ConcreteTypeRepository
        {
            private readonly IInvokeWrapper _wrapper;

            private readonly Invoker _insert;
            private readonly Invoker _containsKey;
            private readonly Invoker _remove;
            private readonly Invoker _getter;
            private readonly object _instance;

            public ConcreteTypeRepository(Type entityType)
            {
                _wrapper = InvokeWrapper.Create();
                var genericType = typeof(Dictionary<,>);
                var instanceType = genericType.MakeGenericType(new Type[] { typeof(string), entityType });
                _instance = Activator.CreateInstance(instanceType);

                var insert_key = _wrapper.Configure(instanceType, "Insert").Single();
                _insert = _wrapper.GetInvoker(insert_key);

                var contains_key = _wrapper.Configure(instanceType, "ContainsKey").Single();
                _containsKey = _wrapper.GetInvoker(contains_key);

                var remove_key = _wrapper.Configure(instanceType, "Remove").Single();
                _remove = _wrapper.GetInvoker(remove_key);

                var p = instanceType.GetProperty("Item", entityType);
                var getter = p.GetGetMethod();
                var get_key = _wrapper.Configure(getter);
                _getter = _wrapper.GetInvoker(get_key);
            }

            public void Insert<T>(string key, T entity)
            {
                _insert.Invoke(_instance, new object[] { key, entity, true });
            }

            public void InsertOrUpdate<T>(string key, T entity)
            {
                if ((bool)_containsKey.Invoke(_instance, key))
                    _remove.Invoke(_instance, key);
                _insert.Invoke(_instance, new object[] { key, entity, true });
            }

            public bool ContainsKey(string key)
            {
                return (bool)_containsKey.Invoke(_instance, key);
            }

            public void Remove<T>(string key)
            {
                _remove.Invoke(_instance, key);
            }

            public T Get<T>(string key)
            {
                return (T)_getter.Invoke(_instance, key);
            }
        }
        private readonly ConcurrentDictionary<Type, ConcreteTypeRepository> _shardedRepositories =
            new ConcurrentDictionary<Type, ConcreteTypeRepository>();

        private ConcreteTypeRepository this[Type type]
        {
            get
            {
                if (_shardedRepositories.ContainsKey(type) == false)
                {
                    var r = new ConcreteTypeRepository(type);
                    _shardedRepositories.AddOrUpdate(type, r, (t, old) => old);
                }
                return _shardedRepositories[type];
            }
        }

        public bool TryAdd<T>(string key, T value)
        {
            try
            {
                this[typeof(T)].Insert<T>(key, value);
                return true;
            }
            catch
            { }
            return false;
        }

        public bool ContainsKey<T>(string key)
        {
            return this[typeof(T)].ContainsKey(key);
        }

        public bool TryRemove<T>(string key)
        {
            try
            {
                this[typeof(T)].Remove<T>(key);
                return true;
            }
            catch
            { }
            return false;
        }

        public void Add<T>(string key, T value)
        {
            this[typeof(T)].Insert<T>(key, value);
        }

        public void Remove<T>(string key)
        {
            this[typeof(T)].Remove<T>(key);
        }

        public T Get<T>(string key)
        {
            return this[typeof(T)].Get<T>(key);
        }

        public static IEverythingStorage Create()
        {
            return new EverythingStorage();
        }

        public void AddOrUpdate<T>(string key, T value)
        {
            this[typeof(T)].InsertOrUpdate<T>(key, value);
        }
    }
}
