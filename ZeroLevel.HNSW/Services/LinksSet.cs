using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    public class LinksSet
    {
        private ConcurrentDictionary<int, HashSet<int>> _set = new ConcurrentDictionary<int, HashSet<int>>();
        internal IDictionary<int, HashSet<int>> Links => _set;
        internal int Count => _set.Count;
        private readonly int _M;

        public LinksSet(int M)
        {
            _M = M;
        }

        internal IEnumerable<(int, int)> FindLinksForId(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id].Select(v => (id, v));
            }
            return Enumerable.Empty<(int, int)>();
        }

        internal IEnumerable<int> FindNeighbors(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id];
            }
            return Enumerable.Empty<int>();
        }

        internal IEnumerable<(int, int)> Items()
        {
            return _set
                .SelectMany(pair => _set[pair.Key]
                    .Select(v => (pair.Key, v)));
        }

        internal void RemoveIndex(int id1, int id2)
        {
            _set[id1].Remove(id2);
            _set[id2].Remove(id1);
        }

        internal bool Add(int id1, int id2)
        {
            if (!_set.ContainsKey(id1))
            {
                _set[id1] = new HashSet<int>(_M + 1);
            }
            if (!_set.ContainsKey(id2))
            {
                _set[id2] = new HashSet<int>(_M + 1);
            }
            var r1 = _set[id1].Add(id2);
            var r2 = _set[id2].Add(id1);
            return r1 || r2;
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
            /*if (reader.ReadBoolean() != false)
            {
                throw new InvalidOperationException("Incompatible format");
            }*/
            _set.Clear();
            _set = null;
            var count = reader.ReadInt32();
            _set = new ConcurrentDictionary<int, HashSet<int>>();
            for (int i = 0; i < count; i++)
            {
                var id = reader.ReadInt32();
                var links_count = reader.ReadInt32();
                _set[id] = new HashSet<int>(links_count);
                for (var l = 0; l < links_count; l++)
                {
                    _set[id].Add(reader.ReadInt32());
                }
            }
        }
    }
}
