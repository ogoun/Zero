using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    // object -> vector -> vectorId
    // HNSW vectorId + vector
    // Map object feature - vectorId
    public class HNSWMap<TFeature>
        : IBinarySerializable
    {
        private ConcurrentDictionary<TFeature, int> _map = new ConcurrentDictionary<TFeature, int>();
        private ConcurrentDictionary<int, TFeature> _reverse_map = new ConcurrentDictionary<int, TFeature>();

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
            this._map = reader.ReadDictionaryAsConcurrent<TFeature, int>();
            this._reverse_map = reader.ReadDictionaryAsConcurrent<int, TFeature>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteDictionary<TFeature, int>(this._map);
            writer.WriteDictionary<int, TFeature>(this._reverse_map);
        }
    }
}
