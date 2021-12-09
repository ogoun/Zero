using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    internal sealed class VectorSet<T>
        : IBinarySerializable
    {
        private List<T> _set = new List<T>();
        private SpinLock _lock = new SpinLock();

        internal T this[int index] => _set[index];
        internal int Count => _set.Count;

        internal int Append(T vector)
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

        internal int[] Append(IEnumerable<T> vectors)
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

        public void Deserialize(IBinaryReader reader)
        {
            int count = reader.ReadInt32();
            _set = new List<T>(count + 1);
            for (int i = 0; i < count; i++)
            {
                _set.Add(reader.ReadCompatible<T>());
            }
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(_set.Count);
            foreach (var r in _set)
            {
                writer.WriteCompatible<T>(r);
            }
        }
    }
}
