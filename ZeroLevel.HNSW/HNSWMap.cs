using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZeroLevel.HNSW
{
    // object -> vector -> vectorId
    // HNSW vectorId + vector
    // Map object feature - vectorId
    public class HNSWMap<TFeature>
    {
        private readonly ConcurrentDictionary<TFeature, int> _map = new ConcurrentDictionary<TFeature, int>();
        private readonly ConcurrentDictionary<int, TFeature> _reverse_map = new ConcurrentDictionary<int, TFeature>();

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
    }
}
