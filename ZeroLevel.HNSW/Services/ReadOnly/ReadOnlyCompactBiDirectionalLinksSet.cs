using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    internal sealed class ReadOnlyCompactBiDirectionalLinksSet
        : IBinarySerializable, IDisposable
    {
        private const int HALF_LONG_BITS = 32;

        private Dictionary<int, int[]> _set = new Dictionary<int, int[]>();

        internal int Count => _set.Count;

        internal IEnumerable<int> FindLinksForId(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id];
            }
            return Enumerable.Empty<int>();
        }

        public void Dispose()
        {
            _set.Clear();
            _set = null;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(false); // false - set without weights
            writer.WriteInt32(_set.Count);
            foreach (var record in _set)
            {
                writer.WriteInt32(record.Key);
                writer.WriteCollection(record.Value);
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            _set.Clear();
            _set = null;
            if (reader.ReadBoolean() == false)
            {
                var count = reader.ReadInt32();
                _set = new Dictionary<int, int[]>(count);
                for (int i = 0; i < count; i++)
                {
                    var key = reader.ReadInt32();
                    var value = reader.ReadInt32Array();
                    _set.Add(key, value);
                }
            }
            else
            {
                var count = reader.ReadInt32();
                _set = new Dictionary<int, int[]>(count);

                // hack, We know that an sortedset has been saved
                long key;
                int id1, id2;
                var prevId = -1;
                var set = new HashSet<int>();

                for (int i = 0; i < count; i++)
                {
                    key = reader.ReadLong();
                    id1 = (int)(key >> HALF_LONG_BITS);
                    id2 = (int)(key - (((long)id1) << HALF_LONG_BITS));                    

                    reader.ReadFloat(); // SKIP

                    if (prevId == -1)
                    {
                        prevId = id1;
                        if (id1 != id2)
                        {
                            set.Add(id2);
                        }
                    }
                    else if (prevId != id1)
                    {
                        _set.Add(prevId, set.ToArray());
                        set.Clear();
                        prevId = id1;
                    }
                    else
                    {
                        if (id1 != id2)
                        {
                            set.Add(id2);
                        }
                    }
                }
                if (set.Count > 0)
                {
                    _set.Add(prevId, set.ToArray());
                    set.Clear();
                }
            }
        }
    }
}
