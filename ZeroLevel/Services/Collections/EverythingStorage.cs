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
        public static IEverythingStorage Create()
        {
            return new EverythingStorage();
        }

        private class ConcreteTypeRepository
        {
            private readonly IInvokeWrapper _wrapper;

            private readonly Invoker _insert;
            private readonly Invoker _containsKey;
            private readonly Invoker _remove;
            private readonly Invoker _getter;
            private readonly Invoker _keys_getter;
            private readonly object _instance;

            public ConcreteTypeRepository(Type entityType)
            {
                _wrapper = InvokeWrapper.Create();
                var genericType = typeof(Dictionary<,>);
                var instanceType = genericType.MakeGenericType(new Type[] { typeof(string), entityType });
                _instance = Activator.CreateInstance(instanceType);

                var insert_key = _wrapper.Configure(instanceType, "Add").Single();
                _insert = _wrapper.GetInvoker(insert_key);

                var contains_key = _wrapper.Configure(instanceType, mi => mi.Name.Equals("ContainsKey") && mi.GetParameters()?.Length == 1).Single();
                _containsKey = _wrapper.GetInvoker(contains_key);

                var remove_key = _wrapper.Configure(instanceType, mi => mi.Name.Equals("Remove") && mi.GetParameters()?.Length == 1).Single();
                _remove = _wrapper.GetInvoker(remove_key);

                var p = instanceType.GetProperty("Item", entityType);
                var getter = p.GetGetMethod();
                var get_key = _wrapper.Configure(getter);
                _getter = _wrapper.GetInvoker(get_key);

                var k = instanceType.GetProperty("Keys", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public| System.Reflection.BindingFlags.NonPublic);
                var keys_getter = k.GetGetMethod();
                var get_keys = _wrapper.Configure(keys_getter);
                _keys_getter = _wrapper.GetInvoker(get_keys);
            }

            public void Insert<T>(string key, T entity)
            {
                _insert.Invoke(_instance, new object[] { key, entity });
            }

            public void InsertOrUpdate<T>(string key, T entity)
            {
                if ((bool)_containsKey.Invoke(_instance, key))
                    _remove.Invoke(_instance, key);
                _insert.Invoke(_instance, new object[] { key, entity });
            }

            public bool ContainsKey(string key)
            {
                return (bool)_containsKey.Invoke(_instance, key);
            }

            public void Remove(string key)
            {
                _remove.Invoke(_instance, key);
            }

            public T Get<T>(string key)
            {
                return (T)_getter.Invoke(_instance, key);
            }

            public IEnumerable<string> Keys()
            {
                return (IEnumerable<string>)_keys_getter.Invoke(_instance);
            }

            public object Get(string key)
            {
                return _getter.Invoke(_instance, key);
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
                this[typeof(T)].Remove(key);
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
            this[typeof(T)].Remove(key);
        }

        public T Get<T>(string key)
        {
            return this[typeof(T)].Get<T>(key);
        }

        public void AddOrUpdate<T>(string key, T value)
        {
            this[typeof(T)].InsertOrUpdate<T>(key, value);
        }

        public bool TryAdd(Type type, string key, object value)
        {
            try
            {
                this[type].Insert(key, value);
                return true;
            }
            catch
            { }
            return false;
        }

        public bool ContainsKey(Type type, string key)
        {
            return this[type].ContainsKey(key);
        }

        public bool TryRemove(Type type, string key)
        {
            try
            {
                this[type].Remove(key);
                return true;
            }
            catch
            { }
            return false;
        }

        public void Add(Type type, string key, object value)
        {
            this[type].Insert(key, value);
        }

        public void AddOrUpdate(Type type, string key, object value)
        {
            this[type].InsertOrUpdate(key, value);
        }

        public void Remove(Type type, string key)
        {
            this[type].Remove(key);
        }

        public object Get(Type type, string key)
        {
            return this[type].Get(key);
        }

        public IEnumerable<string> Keys<T>()
        {
            return this[typeof(T)].Keys();
        }
    }
}