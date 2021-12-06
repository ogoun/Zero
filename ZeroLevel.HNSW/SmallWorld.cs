using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.HNSW
{
    public class ProbabilityLayerNumberGenerator
    {
        private const float DIVIDER = 4.362f;
        private readonly float[] _probabilities;

        public ProbabilityLayerNumberGenerator(int maxLayers, int M)
        {
            _probabilities = new float[maxLayers];
            var probability = 1.0f / DIVIDER;
            for (int i = 0; i < maxLayers; i++)
            {
                _probabilities[i] = probability;
                probability /= DIVIDER;
            }
        }

        public int GetRandomLayer()
        {
            var probability = DefaultRandomGenerator.Instance.NextFloat();
            for (int i = 0; i < _probabilities.Length; i++)
            {
                if (probability > _probabilities[i])
                    return i;
            }
            return 0;
        }
    }

    public class SmallWorld<TItem>
    {
        private readonly NSWOptions<TItem> _options;
        private readonly VectorSet<TItem> _vectors;
        private readonly Layer<TItem>[] _layers;

        private Layer<TItem> EnterPointsLayer => _layers[_layers.Length - 1];
        private Layer<TItem> LastLayer => _layers[0];
        private int EntryPoint = -1;
        private int MaxLayer = -1;
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

        public IEnumerable<(int, TItem[])> Search(TItem vector, int k, HashSet<int> activeNodes = null)
        {
            return Enumerable.Empty<(int, TItem[])>();
        }

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

        public void TestLevelGenerator()
        {
            var levels = new Dictionary<int, float>();
            for (int i = 0; i < 10000; i++)
            {
                var level = _layerLevelGenerator.GetRandomLayer();
                if (levels.ContainsKey(level) == false)
                {
                    levels.Add(level, 1);
                }
                else
                {
                    levels[level] += 1.0f;
                }
            }
            foreach (var pair in levels.OrderBy(l => l.Key))
            {
                Console.WriteLine($"[{pair.Key}]: {pair.Value / 100.0f}% ({pair.Value})");
            }
        }

        #region https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf
        /// <summary>
        /// Algorithm 1
        /// </summary>
        public void INSERT(int q)
        {
            var distance = new Func<int, float>(candidate => _options.Distance(_vectors[q], _vectors[candidate]));

            // W ← ∅ // list for the currently found nearest elements
            IDictionary<int, float> W = new Dictionary<int, float>();
            // ep ← get enter point for hnsw
            var ep = EntryPoint == -1 ? 0 : EntryPoint;
            var epDist = 0.0f;
            // L ← level of ep // top layer for hnsw
            var L = MaxLayer;
            // l ← ⌊-ln(unif(0..1))∙mL⌋ // new element’s level            
            int l = _layerLevelGenerator.GetRandomLayer();
            if (L == -1)
            {
                L = l;
                MaxLayer = l;
            }
            // for lc ← L … l+1
            // Проход с верхнего уровня до уровня где появляется элемент, для нахождения точки входа
            for (int lc = L; lc > l; --lc)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                _layers[lc].RunKnnAtLayer(ep, distance, W, 1);
                // ep ← get the nearest element from W to q
                var nearest = W.OrderBy(p => p.Value).First();
                ep = nearest.Key;
                epDist = nearest.Value;
                W.Clear();
            }
            //for lc ← min(L, l) … 0
            // connecting new node to the small world
            for (int lc = Math.Min(L, l); lc >= 0; --lc)
            {
                // W ← SEARCH - LAYER(q, ep, efConstruction, lc)
                _layers[lc].RunKnnAtLayer(ep, distance, W, _options.EFConstruction);
                // neighbors ← SELECT-NEIGHBORS(q, W, M, lc) // alg. 3 or alg. 4
                var neighbors = SelectBestForConnecting(lc, distance, W);;
                // add bidirectionall connectionts from neighbors to q at layer lc
                // for each e ∈ neighbors // shrink connections if needed
                foreach (var e in neighbors)
                {
                    // eConn ← neighbourhood(e) at layer lc
                    _layers[lc].AddBidirectionallConnectionts(q, e.Key, e.Value);
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
        internal int GetM(int layer)
        {
            return layer == 0 ? 2 * _options.M : _options.M;
        }

        private IDictionary<int, float> SelectBestForConnecting(int layer, Func<int, float> distance, IDictionary<int, float> candidates)
        {
            return _layers[layer].SELECT_NEIGHBORS_SIMPLE(distance, candidates, GetM(layer));
        }

        /// <summary>
        /// Algorithm 5
        /// </summary>
        internal IEnumerable<(int, float)> KNearest(TItem q, int k)
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
                    _layers[layer].RunKnnAtLayer(ep, distance, W, 1);
                    // ep ← get nearest element from W to q
                    ep = W.OrderBy(p => p.Value).First().Key;
                    W.Clear();
                }
                // W ← SEARCH-LAYER(q, ep, ef, lc =0)
                _layers[0].RunKnnAtLayer(ep, distance, W, k);
                // return K nearest elements from W to q
                return W.Select(p => (p.Key, p.Value));
            }
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }
        #endregion
    }
}
