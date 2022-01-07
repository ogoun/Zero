using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    internal class LALLinks
    {
        private ConcurrentDictionary<int, int[]> _set = new ConcurrentDictionary<int, int[]>();
        internal IDictionary<int, int[]> Links => _set;

        private readonly int[] _empty = new int[0];
        internal int Count => _set.Count;

        public LALLinks()
        {
        }

        internal IEnumerable<(int, int)> FindLinksForId(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id].Select(v => (id, v));
            }
            return Enumerable.Empty<(int, int)>();
        }

        internal int[] FindNeighbors(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id];
            }
            return _empty;
        }

        internal IEnumerable<(int, int)> Items()
        {
            return _set
                .SelectMany(pair => _set[pair.Key]
                    .Select(v => (pair.Key, v)));
        }

        public void Dispose()
        {
            _set.Clear();
            _set = null;
        }
        public void Serialize(IBinaryWriter writer)
        {
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
            var count = reader.ReadInt32();
            _set = new ConcurrentDictionary<int, int[]>(1, count);

            for (int i = 0; i < count; i++)
            {
                var id = reader.ReadInt32();
                var links_count = reader.ReadInt32();
                _set[id] = new int[links_count];
                for (int l = 0; l < links_count; l++)
                {
                    _set[id][l] = reader.ReadInt32();
                }
            }
        }
    }
}
