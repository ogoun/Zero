using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Collections
{
    public sealed class FixSizeQueue<T> :
        IFixSizeQueue<T>
    {
        private readonly T[] _array;
        private long _nextIndex;
        private long _startIndex;
        private long _count;
        private readonly object _accessLocker = new object();

        public FixSizeQueue(long capacity)
        {
            if (capacity <= 0)
            {
                capacity = 1024;
            }
            _array = new T[capacity];
            _nextIndex = 0;
            _count = 0;
        }

        /// <summary>
        /// If count is limited when intem adding, oldest item replace with new item
        /// </summary>
        public void Push(T item)
        {
            lock (_accessLocker)
            {
                _array[_nextIndex] = item;
                _nextIndex = (_nextIndex + 1) % _array.Length;
                if (_count < _array.Length)
                {
                    _count++;
                }
                else
                {
                    _startIndex = (_startIndex + 1) % _array.Length;
                }
            }
        }

        public bool Equals(T x, T y)
        {
            if (x == null && y == null) return true;
            if ((object)x == (object)y) return true;
            if (x == null || y == null) return false;
            if (ReferenceEquals(x, y)) return true;
            return x.Equals(y);
        }

        public bool Contains(T item, IComparer<T> comparer = null)
        {
            lock (_accessLocker)
            {
                Func<T, T, bool> eq_func;
                if (comparer == null)
                {
                    eq_func = Equals;
                }
                else
                {
                    eq_func = (x, y) => comparer.Compare(x, y) == 0;
                }
                var cursor = _startIndex;
                if (_count > 0)
                {
                    do
                    {
                        if (eq_func(_array[cursor], item))
                            return true;
                        cursor = (cursor + 1) % _array.Length;
                    } while (cursor != _nextIndex);
                }
            }
            return false;
        }

        public long Count
        {
            get
            {
                return _count;
            }
        }

        public bool TryTake(out T t)
        {
            lock (_accessLocker)
            {
                if (_count > 0)
                {
                    t = _array[_startIndex];
                    _array[_startIndex] = default(T);
                    _startIndex = (_startIndex + 1) % _array.Length;
                    _count--;
                    return true;
                }
            }
            t = default(T);
            return false;
        }

        public T Take()
        {
            T ret;
            lock (_accessLocker)
            {
                if (_count > 0)
                {
                    ret = _array[_startIndex];
                    _array[_startIndex] = default(T);
                    _startIndex = (_startIndex + 1) % _array.Length;
                    _count--;
                    return ret;
                }
            }
            throw new System.Exception("Collection is empty");
        }

        public IEnumerable<T> Dump()
        {
            var dump = new List<T>();
            lock (_accessLocker)
            {
                var cursor = _startIndex;
                if (_count > 0)
                {
                    do
                    {
                        dump.Add(_array[cursor]);
                        cursor = (cursor + 1) % _array.Length;
                    } while (cursor != _nextIndex);
                }
            }
            return dump;
        }
    }
}