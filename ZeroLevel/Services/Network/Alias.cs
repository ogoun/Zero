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

        public void Set(string alias, T address)
        {
            if (_aliases.ContainsKey(alias) == false)
            {
                if (_aliases.TryAdd(alias, new _RoundRobinCollection<T>()))
                {
                    _aliases[alias].Add(address);
                }
            }
            else
            {
                _aliases[alias].Clear();
                _aliases[alias].Add(address);
            }
        }

        public void Set(string alias, IEnumerable<T> addresses)
        {
            if (_aliases.ContainsKey(alias) == false)
            {
                if (_aliases.TryAdd(alias, new _RoundRobinCollection<T>()))
                {
                    foreach (var address in addresses)
                        _aliases[alias].Add(address);
                }
            }
            else
            {
                _aliases[alias].Clear();
                foreach (var address in addresses)
                    _aliases[alias].Add(address);
            }
        }

        public T GetAddress(string alias)
        {
            if (_aliases.ContainsKey(alias) && _aliases[alias].MoveNext())
            {
                return _aliases[alias].Current;
            }
            return default(T);
        }

        public IEnumerable<T> GetAddresses(string alias)
        {
            if (_aliases.ContainsKey(alias) && _aliases[alias].MoveNext())
            {
                return _aliases[alias].GetCurrentSeq();
            }
            return Enumerable.Empty<T>();
        }
    }
}
