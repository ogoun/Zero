using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    /*
    internal struct Link
        : IEquatable<Link>
    {
        public int Id;
        public float Distance;

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Link)
                return this.Equals((Link)obj);
            return false;
        }

        public bool Equals(Link other)
        {
            return this.Id == other.Id;
        }
    }

    public class LinksSetWithCachee
    {
        private ConcurrentDictionary<int, HashSet<Link>> _set = new ConcurrentDictionary<int, HashSet<Link>>();

        internal int Count => _set.Count;

        private readonly int _M;
        private readonly Func<int, int, float> _distance;

        public LinksSetWithCachee(int M, Func<int, int, float> distance)
        {
            _distance = distance;
            _M = M;
        }

        internal IEnumerable<int> FindNeighbors(int id)
        {
            if (_set.ContainsKey(id))
            {
                return _set[id].Select(l=>l.Id);
            }
            return Enumerable.Empty<int>();
        }

        internal void RemoveIndex(int id1, int id2)
        {
            var link1 = new Link { Id = id1 };
            var link2 = new Link { Id = id2 };

            _set[id1].Remove(link2);
            _set[id2].Remove(link1);
        }

        internal bool Add(int id1, int id2, float distance)
        {
            if (!_set.ContainsKey(id1))
            {
                _set[id1] = new HashSet<Link>();
            }
            if (!_set.ContainsKey(id2))
            {
                _set[id2] = new HashSet<Link>();
            }
            var r1 = _set[id1].Add(new Link { Id = id2, Distance = distance });
            var r2 = _set[id2].Add(new Link { Id = id1, Distance = distance });

            //TrimSet(_set[id1]);
            TrimSet(id2, _set[id2]);

            return r1 || r2;
        }

        internal void Trim(int id) => TrimSet(id, _set[id]);

        private void TrimSet(int id, HashSet<Link> set)
        {
            if (set.Count > _M)
            {
                var removeCount = set.Count - _M;
                var removeLinks = set.OrderByDescending(n => n.Distance).Take(removeCount).ToArray();
                foreach (var l in removeLinks)
                {
                    set.Remove(l);
                }
            }
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
    */

    public class LinksSet
    {
        private ConcurrentDictionary<int, HashSet<int>> _set = new ConcurrentDictionary<int, HashSet<int>>();

        internal IDictionary<int, HashSet<int>> Links => _set;

        internal int Count => _set.Count;

        private readonly int _M;
        private readonly Func<int, int, float> _distance;

        public LinksSet(int M, Func<int, int, float> distance)
        {
            _distance = distance;
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

            TrimSet(id1, _set[id1]);
            TrimSet(id2, _set[id2]);

            return r1 || r2;
        }

        internal void Trim(int id) => TrimSet(id, _set[id]);

        private void TrimSet(int id, HashSet<int> set)
        {
            if (set.Count > _M)
            {
                var removeCount = set.Count - _M;
                var removeLinks = set.OrderByDescending(n => _distance(id, n)).Take(removeCount).ToArray();
                foreach (var l in removeLinks)
                {
                    set.Remove(l);
                }
            }
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
