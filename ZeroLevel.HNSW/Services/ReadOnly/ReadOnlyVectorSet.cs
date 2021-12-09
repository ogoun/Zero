using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    internal sealed class ReadOnlyVectorSet<T>
        : IBinarySerializable
    {
        private List<T> _set = new List<T>();

        internal T this[int index] => _set[index];
        internal int Count => _set.Count;

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
