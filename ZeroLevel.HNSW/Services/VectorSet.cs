using System.Collections.Generic;
using System.Threading;

namespace ZeroLevel.HNSW
{
    public class VectorSet<T>
    {
        public IList<T> _set = new List<T>();

        public T this[int index] => _set[index];

        SpinLock _lock = new SpinLock();

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
    }
}
