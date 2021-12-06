using System;
using System.Collections.Generic;
using System.Threading;

namespace ZeroLevel.HNSW
{
    public class VectorSet<T>
    {
        private List<T> _set = new List<T>();
        private SpinLock _lock = new SpinLock();

        public T this[int index] => _set[index];
        public int Count => _set.Count;

        public int Append(T vector)
        {
            bool gotLock = false;
            gotLock = false;
            try
            {
                _lock.Enter(ref gotLock);
                _set.Add(vector);
                return _set.Count - 1;
            }
            finally
            {
                // Only give up the lock if you actually acquired it
                if (gotLock) _lock.Exit();
            }
        }

        public int[] Append(IEnumerable<T> vectors)
        {
            bool gotLock = false;
            int startIndex, endIndex;
            gotLock = false;
            try
            {
                _lock.Enter(ref gotLock);
                startIndex = _set.Count;
                _set.AddRange(vectors);
                endIndex = _set.Count;                
            }
            finally
            {
                // Only give up the lock if you actually acquired it
                if (gotLock) _lock.Exit();
            }
            var ids = new int[endIndex - startIndex];
            for (int i = startIndex, j = 0; i < endIndex; i++, j++)
            {
                ids[j] = i;
            }
            return ids;
        }
    }
}
