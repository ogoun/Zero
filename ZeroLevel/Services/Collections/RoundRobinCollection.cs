﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.Services.Collections
{
    /// <summary>
    /// Collection return new seq every iteration
    /// Sample. Original: [1,2,3]. Iteration #1: [1, 2, 3]. Iteration #2: [2, 3, 1]. Iteration #3: [3, 1, 2]. Iteration #4: [1, 2, 3]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class RoundRobinCollection<T> :
        IDisposable
    {
        private readonly List<T> _collection =
            new List<T>();

        private int _index = -1;

        private readonly ReaderWriterLockSlim _lock =
            new ReaderWriterLockSlim();

        public int Count { get { return _collection.Count; } }

        public RoundRobinCollection() { }
        public RoundRobinCollection(IEnumerable<T> items)
        {
            if (items != null && items.Any())
            {
                _collection.AddRange(items);
                _index = 0;
            }
        }

        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_collection.Contains(item))
                {
                    _collection.Add(item);
                    if (_index == -1) _index = 0;
                }
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

        public IEnumerable<T> MoveNextSeq()
        {
            _lock.EnterReadLock();
            try
            {
                if (_collection.Count > 0)
                {
                    _index = Interlocked.Increment(ref _index) % _collection.Count;
                    int p = 0;
                    for (int i = _index; i < _collection.Count; i++, p++)
                    {
                        yield return _collection[i];
                    }
                    for (int i = 0; i < _index; i++, p++)
                    {
                        yield return _collection[i];
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool MoveNextAndHandle(Action<T> handler)
        {
            _lock.EnterReadLock();
            try
            {
                if (_collection.Count > 0)
                {
                    _index = Interlocked.Increment(ref _index) % _collection.Count;
                    handler.Invoke(Current);
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
                return (_index == -1 ? default(T) : _collection[_index])!;
            }
        }

        public IEnumerable<T> Source { get { return _collection; } }

        public IEnumerable<T> GetCurrentSeq()
        {
            _lock.EnterReadLock();
            try
            {
                int p = 0;
                for (int i = _index; i < _collection.Count; i++, p++)
                {
                    yield return _collection[i];
                }
                for (int i = 0; i < _index; i++, p++)
                {
                    yield return _collection[i];
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<T> Find(Func<T, bool> selector)
        {
            _lock.EnterReadLock();
            try
            {
                var arr = new List<T>(_collection.Count);
                for (int i = _index; i < _collection.Count; i++)
                {
                    if (selector(_collection[i]))
                    {
                        arr.Add(_collection[i]);
                    }
                }
                return arr;
            }
            finally
            {
                _lock.ExitReadLock();
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
}