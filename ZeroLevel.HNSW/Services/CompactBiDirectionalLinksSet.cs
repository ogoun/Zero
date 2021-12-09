using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.HNSW
{
    internal sealed class CompactBiDirectionalLinksSet
        : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        private const int HALF_LONG_BITS = 32;

        private SortedList<long, float> _set = new SortedList<long, float>();

        public (int, int) this[int index]
        {
            get
            {
                var k = _set.Keys[index];
                var id1 = (int)(k >> HALF_LONG_BITS);
                var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));
                return (id1, id2);
            }
        }

        public int Count => _set.Count;

        /// <summary>
        /// Разрывает связи id1 - id2 и id2 - id1, и строит новые id1 - id, id - id1
        /// </summary>
        public void Relink(int id1, int id2, int id, float distance)
        {
            long k1old = (((long)(id1)) << HALF_LONG_BITS) + id2;
            long k2old = (((long)(id2)) << HALF_LONG_BITS) + id1;

            long k1new = (((long)(id1)) << HALF_LONG_BITS) + id;
            long k2new = (((long)(id)) << HALF_LONG_BITS) + id1;

            _rwLock.EnterWriteLock();
            try
            {
                _set.Remove(k1old);
                _set.Remove(k2old);
                if (!_set.ContainsKey(k1new))
                    _set.Add(k1new, distance);
                if (!_set.ContainsKey(k2new))
                    _set.Add(k2new, distance);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Разрывает связи id1 - id2 и id2 - id1, и строит новые id1 - id, id - id1, id2 - id, id - id2
        /// </summary>
        public void Relink(int id1, int id2, int id, float distanceToId1, float distanceToId2)
        {
            long k_id1_id2 = (((long)(id1)) << HALF_LONG_BITS) + id2;
            long k_id2_id1 = (((long)(id2)) << HALF_LONG_BITS) + id1;

            long k_id_id1 = (((long)(id)) << HALF_LONG_BITS) + id1;
            long k_id1_id = (((long)(id1)) << HALF_LONG_BITS) + id;

            long k_id_id2 = (((long)(id)) << HALF_LONG_BITS) + id2;
            long k_id2_id = (((long)(id2)) << HALF_LONG_BITS) + id;

            _rwLock.EnterWriteLock();
            try
            {
                _set.Remove(k_id1_id2);
                _set.Remove(k_id2_id1);
                if (!_set.ContainsKey(k_id_id1))
                {
                    _set.Add(k_id_id1, distanceToId1);
                }
                if (!_set.ContainsKey(k_id1_id))
                {
                    _set.Add(k_id1_id, distanceToId1);
                }
                if (!_set.ContainsKey(k_id_id2))
                {
                    _set.Add(k_id_id2, distanceToId2);
                }
                if (!_set.ContainsKey(k_id2_id))
                {
                    _set.Add(k_id2_id, distanceToId2);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public IEnumerable<(int, int, float)> FindLinksForId(int id)
        {
            _rwLock.EnterReadLock();
            try
            {
                foreach (var (k, v) in Search(_set, id))
                {
                    var id1 = (int)(k >> HALF_LONG_BITS);
                    var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));
                    yield return (id1, id2, v);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public IEnumerable<(int, int, float)> Items()
        {
            _rwLock.EnterReadLock();
            try
            {
                foreach (var pair in _set)
                {
                    var id1 = (int)(pair.Key >> HALF_LONG_BITS);
                    var id2 = (int)(pair.Key - (((long)id1) << HALF_LONG_BITS));
                    yield return (id1, id2, pair.Value);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public void RemoveIndex(int id)
        {
            long[] forward;
            long[] backward;
            _rwLock.EnterReadLock();
            try
            {
                forward = Search(_set, id).Select(pair => pair.Item1).ToArray();
                backward = forward.Select(k =>
                {
                    var id1 = k >> HALF_LONG_BITS;
                    var id2 = k - (id1 << HALF_LONG_BITS);
                    return (id2 << HALF_LONG_BITS) + id1;
                }).ToArray();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            _rwLock.EnterWriteLock();
            try
            {
                foreach (var k in forward)
                {
                    _set.Remove(k);
                }
                foreach (var k in backward)
                {
                    _set.Remove(k);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public bool Add(int id1, int id2, float distance)
        {
            _rwLock.EnterWriteLock();
            try
            {
                long k1 = (((long)(id1)) << HALF_LONG_BITS) + id2;
                long k2 = (((long)(id2)) << HALF_LONG_BITS) + id1;
                if (_set.ContainsKey(k1) == false)
                {
                    _set.Add(k1, distance);
                    if (k1 != k2)
                    {
                        _set.Add(k2, distance);
                    }
                    return true;
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return false;
        }

        static IEnumerable<(long, float)> Search(SortedList<long, float> set, int index)
        {
            long k = ((long)index) << HALF_LONG_BITS;
            int left = 0;
            int right = set.Count - 1;
            int mid;
            long test;
            while (left < right)
            {
                mid = (right + left) / 2;
                test = (set.Keys[mid] >> HALF_LONG_BITS) << HALF_LONG_BITS;

                if (left == mid || right == mid)
                {
                    if (test == k)
                    {
                        return SearchByPosition(set, k, mid);
                    }
                    break;
                }
                if (test < k)
                {
                    left = mid;
                }
                else
                {
                    if (test == k)
                    {
                        return SearchByPosition(set, k, mid);
                    }
                    else
                    {
                        right = mid;
                    }
                }
            }
            return Enumerable.Empty<(long, float)>();
        }

        static IEnumerable<(long, float)> SearchByPosition(SortedList<long, float> set, long k, int position)
        {
            var start = position;
            var end = position;
            do
            {
                position--;
            } while (position >= 0 && ((set.Keys[position] >> HALF_LONG_BITS) << HALF_LONG_BITS) == k);
            start = position + 1;
            position = end + 1;
            while (position < set.Count && ((set.Keys[position] >> HALF_LONG_BITS) << HALF_LONG_BITS) == k)
            {
                position++;
            }
            end = position - 1;
            for (int i = start; i <= end; i++)
            {
                yield return (set.Keys[i], set.Values[i]);
            }
        }

        public void Dispose()
        {
            _rwLock.Dispose();
            _set.Clear();
            _set = null;
        }
    }
}
