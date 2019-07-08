using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.Network
{
    public sealed class AliasSet<T>
    {
        public sealed class _RoundRobinCollection<T> :
        IDisposable
        {
            private readonly List<T> _collection =
                new List<T>();

            private int _index = -1;

            private readonly ReaderWriterLockSlim _lock =
                new ReaderWriterLockSlim();

            public int Count { get { return _collection.Count; } }

            public void Add(T item)
            {
                _lock.EnterWriteLock();
                try
                {
                    _collection.Add(item);
                    if (_index == -1) _index = 0;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void Remove(T item)
            {
                _lock.EnterWriteLock();
                try
                {
                    _collection.Remove(item);
                    if (_index >= _collection.Count)
                    {
                        if (_collection.Count == 0) _index = -1;
                        else _index = 0;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public bool Contains(T item)
            {
                _lock.EnterReadLock();
                try
                {
                    return _collection.Contains(item);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public bool MoveNext()
            {
                _lock.EnterReadLock();
                try
                {
                    if (_collection.Count > 0)
                    {
                        _index = Interlocked.Increment(ref _index) % _collection.Count;
                        return true;
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
                return false;
            }

            public T Current
            {
                get
                {
                    return _index == -1 ? default(T) : _collection[_index];
                }
            }

            public void Clear()
            {
                _collection.Clear();
                _index = -1;
            }

            public IEnumerable<T> GetCurrentSeq()
            {
                _lock.EnterReadLock();
                try
                {
                    var arr = new T[_collection.Count];
                    int p = 0;
                    for (int i = _index; i < _collection.Count; i++, p++)
                    {
                        arr[p] = _collection[i];
                    }
                    for (int i = 0; i < _index; i++, p++)
                    {
                        arr[p] = _collection[i];
                    }
                    return arr;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public void Dispose()
            {
                _collection.Clear();
                _lock.Dispose();
            }
        }

        private readonly ConcurrentDictionary<string, _RoundRobinCollection<T>> _aliases = new ConcurrentDictionary<string, _RoundRobinCollection<T>>();

        public bool Contains(string key) => _aliases.ContainsKey(key);

        public void Append(string key, T address)
        {
            if (_aliases.ContainsKey(key) == false)
            {
                if (_aliases.TryAdd(key, new _RoundRobinCollection<T>()))
                {
                    _aliases[key].Add(address);
                }
            }
            else
            {
                _aliases[key].Add(address);
            }
        }

        public void Append(string key, IEnumerable<T> addresses)
        {
            if (_aliases.ContainsKey(key) == false)
            {
                if (_aliases.TryAdd(key, new _RoundRobinCollection<T>()))
                {
                    foreach (var address in addresses)
                    {
                        _aliases[key].Add(address);
                    }
                }
            }
            else
            {
                foreach (var address in addresses)
                {
                    _aliases[key].Add(address);
                }
            }
        }

        public void Update(string key, T address)
        {
            if (_aliases.ContainsKey(key) == false)
            {
                if (_aliases.TryAdd(key, new _RoundRobinCollection<T>()))
                {
                    _aliases[key].Add(address);
                }
            }
            else
            {
                _aliases[key].Clear();
                _aliases[key].Add(address);
            }
        }

        public void Update(string key, IEnumerable<T> addresses)
        {
            if (_aliases.ContainsKey(key) == false)
            {
                if (_aliases.TryAdd(key, new _RoundRobinCollection<T>()))
                {
                    foreach (var address in addresses)
                    {
                        _aliases[key].Add(address);
                    }
                }
            }
            else
            {
                _aliases[key].Clear();
                foreach (var address in addresses)
                {
                    _aliases[key].Add(address);
                }
            }
        }

        public T Get(string key)
        {
            if (_aliases.ContainsKey(key) && _aliases[key].MoveNext())
            {
                return _aliases[key].Current;
            }
            return default(T);
        }

        public IEnumerable<T> GetAll(string key)
        {
            if (_aliases.ContainsKey(key) && _aliases[key].MoveNext())
            {
                return _aliases[key].GetCurrentSeq();
            }
            return Enumerable.Empty<T>();
        }
    }
}
