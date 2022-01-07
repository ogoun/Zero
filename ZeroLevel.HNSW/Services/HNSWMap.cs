using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    // object -> vector -> vectorId
    // HNSW vectorId + vector
    // Map object feature - vectorId
    public class HNSWMap<TFeature>
        : IBinarySerializable
    {
        private Dictionary<TFeature, int> _map;
        private Dictionary<int, TFeature> _reverse_map;

        public int this[TFeature feature] => _map.GetValueOrDefault(feature);
        public HNSWMap(int capacity = -1)
        {
            if (capacity > 0)
            {
                _map = new Dictionary<TFeature, int>(capacity);
                _reverse_map = new Dictionary<int, TFeature>(capacity);
            }
            else
            {
                _map = new Dictionary<TFeature, int>();
                _reverse_map = new Dictionary<int, TFeature>();

            }
        }

        public HNSWMap(Stream stream)
        {
            using (var reader = new MemoryStreamReader(stream))
            {
                Deserialize(reader);
            }
        }

        public void Append(TFeature feature, int vectorId)
        {
            _map[feature] = vectorId;
            _reverse_map[vectorId] = feature;
        }

        public IEnumerable<int> ConvertFeaturesToIds(IEnumerable<TFeature> features)
        {
            int id;
            foreach (var feature in features)
            {
                if (_map.TryGetValue(feature, out id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<TFeature> ConvertIdsToFeatures(IEnumerable<int> ids)
        {
            TFeature feature;
            foreach (var id in ids)
            {
                if (_reverse_map.TryGetValue(id, out feature))
                {
                    yield return feature;
                }
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._map = reader.ReadDictionary<TFeature, int>();
            this._reverse_map = reader.ReadDictionary<int, TFeature>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteDictionary<TFeature, int>(this._map);
            writer.WriteDictionary<int, TFeature>(this._reverse_map);
        }
    }
}
