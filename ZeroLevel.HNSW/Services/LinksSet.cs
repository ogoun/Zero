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

        private const int HALF_LONG_BITS = 32;
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(false); // true - set with weights
            writer.WriteInt32(_set.Sum(pair => pair.Value.Count));
            foreach (var record in _set)
            {
                var id = record.Key;
                foreach (var r in record.Value)
                {
                    var key = (((long)(id)) << HALF_LONG_BITS) + r;
                    writer.WriteLong(key);
                }
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
            _set = new ConcurrentDictionary<int, HashSet<int>>();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadLong();

                var id1 = (int)(key >> HALF_LONG_BITS);
                var id2 = (int)(key - (((long)id1) << HALF_LONG_BITS));

                if (!_set.ContainsKey(id1))
                {
                    _set[id1] = new HashSet<int>();
                }
                _set[id1].Add(id2);
            }
        }
    }
}
