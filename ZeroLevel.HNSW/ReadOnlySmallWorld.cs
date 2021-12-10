using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    public class ReadOnlySmallWorld<TItem>
    {
        private readonly NSWReadOnlyOption<TItem> _options;
        private ReadOnlyVectorSet<TItem> _vectors;
        private ReadOnlyLayer<TItem>[] _layers;
        private int EntryPoint = 0;
        private int MaxLayer = 0;

        private ReadOnlySmallWorld() { }

        internal ReadOnlySmallWorld(NSWReadOnlyOption<TItem> options, Stream stream)
        {
            _options = options;
            Deserialize(stream);
        }

        /// <summary>
        /// Search in the graph K for vectors closest to a given vector
        /// </summary>
        /// <param name="vector">Given vector</param>
        /// <param name="k">Count of elements for search</param>
        /// <param name="activeNodes"></param>
        /// <returns></returns>
        public IEnumerable<(int, TItem, float)> Search(TItem vector, int k)
        {
            foreach (var pair in KNearest(vector, k))
            {
                yield return (pair.Item1, _vectors[pair.Item1], pair.Item2);
            }
        }

        public IEnumerable<(int, TItem, float)> Search(TItem vector, int k, SearchContext context)
        {
            if (context == null)
            {
                foreach (var pair in KNearest(vector, k))
                {
                    yield return (pair.Item1, _vectors[pair.Item1], pair.Item2);
                }
            }
            else
            {
                foreach (var pair in KNearest(vector, k, context))
                {
                    yield return (pair.Item1, _vectors[pair.Item1], pair.Item2);
                }
            }
        }

        /// <summary>
        /// Algorithm 5
        /// </summary>
        private IEnumerable<(int, float)> KNearest(TItem q, int k)
        {
            if (_vectors.Count == 0)
            {
                return Enumerable.Empty<(int, float)>();
            }
            var distance = new Func<int, float>(candidate => _options.Distance(q, _vectors[candidate]));

            // W ← ∅ // set for the current nearest elements
            var W = new Dictionary<int, float>(k + 1);
            // ep ← get enter point for hnsw
            var ep = EntryPoint;
            // L ← level of ep // top layer for hnsw
            var L = MaxLayer;
            // for lc ← L … 1
            for (int layer = L; layer > 0; --layer)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                _layers[layer].KNearestAtLayer(ep, distance, W, 1);
                // ep ← get nearest element from W to q
                ep = W.OrderBy(p => p.Value).First().Key;
                W.Clear();
            }
            // W ← SEARCH-LAYER(q, ep, ef, lc =0)
            _layers[0].KNearestAtLayer(ep, distance, W, k);
            // return K nearest elements from W to q
            return W.Select(p => (p.Key, p.Value));
        }

        private IEnumerable<(int, float)> KNearest(TItem q, int k, SearchContext context)
        {
            if (_vectors.Count == 0)
            {
                return Enumerable.Empty<(int, float)>();
            }
            var distance = new Func<int, float>(candidate => _options.Distance(q, _vectors[candidate]));

            // W ← ∅ // set for the current nearest elements
            var W = new Dictionary<int, float>(k + 1);
            // ep ← get enter point for hnsw
            var ep = EntryPoint;
            // L ← level of ep // top layer for hnsw
            var L = MaxLayer;
            // for lc ← L … 1
            for (int layer = L; layer > 0; --layer)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                _layers[layer].KNearestAtLayer(ep, distance, W, 1);
                // ep ← get nearest element from W to q
                ep = W.OrderBy(p => p.Value).First().Key;
                W.Clear();
            }
            // W ← SEARCH-LAYER(q, ep, ef, lc =0)
            _layers[0].KNearestAtLayer(ep, distance, W, k, context);
            // return K nearest elements from W to q
            return W.Select(p => (p.Key, p.Value));
        }

        public void Serialize(Stream stream)
        {
            using (var writer = new MemoryStreamWriter(stream))
            {
                writer.WriteInt32(EntryPoint);
                writer.WriteInt32(MaxLayer);
                _vectors.Serialize(writer);
                writer.WriteInt32(_layers.Length);
                foreach (var l in _layers)
                {
                    l.Serialize(writer);
                }
            }
        }

        public void Deserialize(Stream stream)
        {
            using (var reader = new MemoryStreamReader(stream))
            {
                this.EntryPoint = reader.ReadInt32();
                this.MaxLayer = reader.ReadInt32();
                _vectors = new ReadOnlyVectorSet<TItem>();
                _vectors.Deserialize(reader);
                var countLayers = reader.ReadInt32();
                _layers = new ReadOnlyLayer<TItem>[countLayers];
                for (int i = 0; i < countLayers; i++)
                {
                    _layers[i] = new ReadOnlyLayer<TItem>(_vectors);
                    _layers[i].Deserialize(reader);
                }
            }
        }
    }
}
