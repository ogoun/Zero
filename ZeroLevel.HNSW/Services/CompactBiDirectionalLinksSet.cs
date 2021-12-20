using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    internal sealed class CompactBiDirectionalLinksSet
        : IBinarySerializable, IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        private const int HALF_LONG_BITS = 32;

        private SortedList<long, float> _set = new SortedList<long, float>();

        internal SortedList<long, float> Links => _set;

        internal (int, int) this[int index]
        {
            get
            {
                var k = _set.Keys[index];
                var id1 = (int)(k >> HALF_LONG_BITS);
                var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));
                return (id1, id2);
            }
        }

        internal int Count => _set.Count;

        internal IEnumerable<(int, int, float)> FindLinksForId(int id)
        {
            _rwLock.EnterReadLock();
            try
            {
                if (_set.Count == 1)
                {
                    var k = _set.Keys[0];
                    var v = _set[k];
                    var id1 = (int)(k >> HALF_LONG_BITS);
                    var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));
                    if (id1 == id) yield return (id, id2, v);
                    else if (id2 == id) yield return (id1, id, v);
                }
                else if (_set.Count > 1)
                {
                    foreach (var (k, v) in Search(_set, id))
                    {
                        var id1 = (int)(k >> HALF_LONG_BITS);
                        var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));
                        yield return (id1, id2, v);
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        internal IEnumerable<(int, int, float)> Items()
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

        internal void RemoveIndex(int id1, int id2)
        {
            long k1 = (((long)(id1)) << HALF_LONG_BITS) + id2;
            long k2 = (((long)(id2)) << HALF_LONG_BITS) + id1;
            _rwLock.EnterWriteLock();
            try
            {
                if (_set.ContainsKey(k1))
                {
                    _set.Remove(k1);
                }
                if (_set.ContainsKey(k2))
                {
                    _set.Remove(k2);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        internal bool Add(int id1, int id2, float distance)
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

        /*
         
function binary_search(A, n, T) is
    L := 0
    R := n − 1
    while L ≤ R do
        m := floor((L + R) / 2)
        if A[m] < T then
            L := m + 1
        else if A[m] > T then
            R := m − 1
        else:
            return m
    return unsuccessful

         */

        private static IEnumerable<(long, float)> Search(SortedList<long, float> set, int index)
        {
            long k = ((long)index) << HALF_LONG_BITS;   // T
            int left = 0;
            int right = set.Count - 1;
            int mid;
            long test;
            while (left <= right)
            {
                mid = (int)Math.Floor((right + left) / 2d);
                test = (set.Keys[mid] >> HALF_LONG_BITS) << HALF_LONG_BITS; // A[m]

                if (test < k)
                {
                    left = mid + 1;
                }
                else if (test > k)
                {
                    right = mid - 1;
                }
                else
                {
                    return SearchByPosition(set, k, mid);
                }
            }
            return Enumerable.Empty<(long, float)>();
        }

        private static IEnumerable<(long, float)> SearchByPosition(SortedList<long, float> set, long k, int position)
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

        public Histogram CalculateHistogram(HistogramMode mode)
        {
            return new Histogram(mode, _set.Values);
        }

        internal float Distance(int id1, int id2)
        {
            long k = (((long)(id1)) << HALF_LONG_BITS) + id2;
            if (_set.ContainsKey(k))
            {
                return _set[k];
            }
            return float.MaxValue;
        }

        public void Dispose()
        {
            _rwLock.Dispose();
            _set.Clear();
            _set = null;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(true); // true - set with weights
            writer.WriteInt32(_set.Count);
            foreach (var record in _set)
            {
                writer.WriteLong(record.Key);
                writer.WriteFloat(record.Value);
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            if (reader.ReadBoolean() == false)
            {
                throw new InvalidOperationException("Incompatible data format. The set does not contain weights.");
            }
            _set.Clear();
            _set = null;
            var count = reader.ReadInt32();
            _set = new SortedList<long, float>(count + 1);
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadLong();
                var value = reader.ReadFloat();
                _set.Add(key, value);
            }
        }
    }
}
