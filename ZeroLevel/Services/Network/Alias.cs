using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ZeroLevel.Network
{
    public sealed class AliasSet
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

            public void Dispose()
            {
                _collection.Clear();
                _lock.Dispose();
            }
        }

        private readonly ConcurrentDictionary<string, _RoundRobinCollection<string>> _aliases = new ConcurrentDictionary<string, _RoundRobinCollection<string>>();

        public void Set(string alias, string address)
        {
            if (_aliases.ContainsKey(alias) == false)
            {
                if (_aliases.TryAdd(alias, new _RoundRobinCollection<string>()))
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

        public void Set(string alias, IEnumerable<string> addresses)
        {
            if (_aliases.ContainsKey(alias) == false)
            {
                if (_aliases.TryAdd(alias, new _RoundRobinCollection<string>()))
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

        public string GetAddress(string alias)
        {
            if (_aliases.ContainsKey(alias) && _aliases[alias].MoveNext())
            {
                return _aliases[alias].Current;
            }
            return null;
        }
    }
}
