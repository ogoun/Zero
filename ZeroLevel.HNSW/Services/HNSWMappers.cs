using System;
using System.Collections.Generic;

namespace ZeroLevel.HNSW
{
    public class HNSWMappers<TFeature>
    {
        private readonly IDictionary<int, HNSWMap<TFeature>> _mappers = new Dictionary<int, HNSWMap<TFeature>>();
        private readonly Func<TFeature, int> _bucketFunction;
        public HNSWMappers(Func<TFeature, int> bucketFunction)
        {
            _bucketFunction = bucketFunction;
        }

        public void Append(HNSWMap<TFeature> map, int c)
        {
            _mappers.Add(c, map);
        }

        public IEnumerable<TFeature> ConvertIdsToFeatures(int c, IEnumerable<int> ids)
        {
            foreach (var feature in _mappers[c].ConvertIdsToFeatures(ids))
            {
                yield return feature;
            }
        }

        public IDictionary<int, SearchContext> CreateContext(IEnumerable<TFeature> activeNodes, IEnumerable<TFeature> entryPoints)
        {
            var actives = new Dictionary<int, List<int>>();
            var entries = new Dictionary<int, List<int>>();
            if (activeNodes != null)
            {
                foreach (var node in activeNodes)
                {
                    var c = _bucketFunction(node);
                    if (_mappers.ContainsKey(c))
                    {
                        if (actives.ContainsKey(c) == false)
                        {
                            actives.Add(c, new List<int>());
                        }
                        actives[c].Add(_mappers[c][node]);
                    }
                }
            }
            if (entryPoints != null)
            {
                foreach (var entryPoint in entryPoints)
                {
                    var c = _bucketFunction(entryPoint);
                    if (_mappers.ContainsKey(c))
                    {
                        if (entries.ContainsKey(c) == false)
                        {
                            entries.Add(c, new List<int>());
                        }
                        entries[c].Add(_mappers[c][entryPoint]);
                    }
                }
            }
            var result = new Dictionary<int, SearchContext>();
            foreach (var pair in _mappers)
            {
                var active = actives.GetValueOrDefault(pair.Key);
                var entry = entries.GetValueOrDefault(pair.Key);
                result.Add(pair.Key, new SearchContext().SetActiveNodes(active).SetEntryPointsNodes(entry));
            }
            return result;
        }
    }
}
