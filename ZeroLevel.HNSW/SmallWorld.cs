using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ZeroLevel.HNSW.Services;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    public class SmallWorld<TItem>
    {
        private readonly NSWOptions<TItem> _options;
        private readonly VectorSet<TItem> _vectors;
        private Layer<TItem>[] _layers;
        private int EntryPoint = 0;
        private int MaxLayer = 0;
        private readonly ProbabilityLayerNumberGenerator _layerLevelGenerator;
        private ReaderWriterLockSlim _lockGraph = new ReaderWriterLockSlim();

        public SmallWorld(NSWOptions<TItem> options)
        {
            _options = options;
            _vectors = new VectorSet<TItem>();
            _layers = new Layer<TItem>[_options.LayersCount];
            _layerLevelGenerator = new ProbabilityLayerNumberGenerator(_options.LayersCount, _options.M);
            for (int i = 0; i < _options.LayersCount; i++)
            {
                _layers[i] = new Layer<TItem>(_options, _vectors);
            }
        }

        internal SmallWorld(NSWOptions<TItem> options, Stream stream)
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

        public IEnumerable<(int, TItem, float)> Search(int k, SearchContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            else
            {
                foreach (var pair in KNearest(k, context))
                {
                    yield return (pair.Item1, _vectors[pair.Item1], pair.Item2);
                }
            }
        }

        /// <summary>
        /// Adding vectors batch
        /// </summary>
        /// <param name="vectors">Vectors</param>
        /// <returns>Vector identifiers in a graph</returns>
        public int[] AddItems(IEnumerable<TItem> vectors)
        {
            _lockGraph.EnterWriteLock();
            try
            {
                var ids = _vectors.Append(vectors);
                for (int i = 0; i < ids.Length; i++)
                {
                    INSERT(ids[i]);
                }
                return ids;
            }
            finally
            {
                _lockGraph.ExitWriteLock();
            }
        }

        #region https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf
        /// <summary>
        /// Algorithm 1
        /// </summary>
        private void INSERT(int q)
        {
            var distance = new Func<int, float>(candidate => _options.Distance(_vectors[q], _vectors[candidate]));
            // W ← ∅ // list for the currently found nearest elements
            IDictionary<int, float> W = new Dictionary<int, float>();
            // ep ← get enter point for hnsw
            var ep = EntryPoint;
            var epDist = distance(ep);
            // L ← level of ep // top layer for hnsw
            var L = MaxLayer;
            // l ← ⌊-ln(unif(0..1))∙mL⌋ // new element’s level            
            int l = _layerLevelGenerator.GetRandomLayer();
            // for lc ← L … l+1
            // Проход с верхнего уровня до уровня где появляется элемент, для нахождения точки входа
            for (int lc = L; lc > l; --lc)
            {
                if (_layers[lc].HasLinks == false)
                {
                    _layers[lc].Append(q);
                    ep = q;
                }
                else
                {
                    // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                    _layers[lc].KNearestAtLayer(ep, distance, W, 1);
                    // ep ← get the nearest element from W to q
                    var nearest = W.OrderBy(p => p.Value).First();
                    ep = nearest.Key;
                    epDist = nearest.Value;
                    W.Clear();
                }
            }
            //for lc ← min(L, l) … 0
            // connecting new node to the small world
            for (int lc = Math.Min(L, l); lc >= 0; --lc)
            {
                if (_layers[lc].HasLinks == false)
                {
                    _layers[lc].Append(q);
                    ep = q;
                }
                else
                {
                    // W ← SEARCH - LAYER(q, ep, efConstruction, lc)
                    _layers[lc].KNearestAtLayer(ep, distance, W, _options.EFConstruction);
                    // neighbors ← SELECT-NEIGHBORS(q, W, M, lc) // alg. 3 or alg. 4
                    var neighbors = SelectBestForConnecting(lc, distance, W);
                    // add bidirectionall connectionts from neighbors to q at layer lc
                    // for each e ∈ neighbors // shrink connections if needed
                    foreach (var e in neighbors)
                    {
                        // eConn ← neighbourhood(e) at layer lc
                        _layers[lc].AddBidirectionallConnections(q, e.Key, e.Value, lc == 0);
                        // if distance from newNode to newNeighbour is better than to bestPeer => update bestPeer
                        if (e.Value < epDist)
                        {
                            ep = e.Key;
                            epDist = e.Value;
                        }
                    }
                    // ep ← W
                    ep = W.OrderBy(p => p.Value).First().Key;
                    W.Clear();
                }
            }
            // if l > L
            if (l > L)
            {
                // set enter point for hnsw to q
                L = l;
                MaxLayer = l;
                EntryPoint = ep;
            }
        }

        /// <summary>
        /// Get maximum allowed connections for the given level.
        /// </summary>
        /// <remarks>
        /// Article: Section 4.1:
        /// "Selection of the Mmax0 (the maximum number of connections that an element can have in the zero layer) also
        /// has a strong influence on the search performance, especially in case of high quality(high recall) search.
        /// Simulations show that setting Mmax0 to M(this corresponds to kNN graphs on each layer if the neighbors
        /// selection heuristic is not used) leads to a very strong performance penalty at high recall.
        /// Simulations also suggest that 2∙M is a good choice for Mmax0;
        /// setting the parameter higher leads to performance degradation and excessive memory usage."
        /// </remarks>
        /// <param name="layer">The level of the layer.</param>
        /// <returns>The maximum number of connections.</returns>
        private int GetM(int layer)
        {
            return layer == 0 ? 2 * _options.M : _options.M;
        }

        private IDictionary<int, float> SelectBestForConnecting(int layer, Func<int, float> distance, IDictionary<int, float> candidates)
        {
            if (_options.SelectionHeuristic == NeighbourSelectionHeuristic.SelectSimple)
                return _layers[layer].SELECT_NEIGHBORS_SIMPLE(distance, candidates, GetM(layer));
            return _layers[layer].SELECT_NEIGHBORS_HEURISTIC(distance, candidates, GetM(layer));
        }

        /// <summary>
        /// Algorithm 5
        /// </summary>
        private IEnumerable<(int, float)> KNearest(TItem q, int k)
        {
            _lockGraph.EnterReadLock();
            try
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
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }
        private IEnumerable<(int, float)> KNearest(TItem q, int k, SearchContext context)
        {
            _lockGraph.EnterReadLock();
            try
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
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }

        private IEnumerable<(int, float)> KNearest(int k, SearchContext context)
        {
            _lockGraph.EnterReadLock();
            try
            {
                if (_vectors.Count == 0)
                {
                    return Enumerable.Empty<(int, float)>();
                }
                var distance = new Func<int, int, float>((id1, id2) => _options.Distance(_vectors[id1], _vectors[id2]));

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
                    _layers[layer].KNearestAtLayer(ep, W, 1, context);
                    // ep ← get nearest element from W to q
                    ep = W.OrderBy(p => p.Value).First().Key;
                    W.Clear();
                }
                // W ← SEARCH-LAYER(q, ep, ef, lc =0)
                _layers[0].KNearestAtLayer(ep, W, k, context);
                // return K nearest elements from W to q
                return W.Select(p => (p.Key, p.Value));
            }
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }
        #endregion

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
                _vectors.Deserialize(reader);
                var countLayers = reader.ReadInt32();
                _layers = new Layer<TItem>[countLayers];
                for (int i = 0; i < countLayers; i++)
                {
                    _layers[i] = new Layer<TItem>(_options, _vectors);
                    _layers[i].Deserialize(reader);
                }
            }
        }
    }
}
