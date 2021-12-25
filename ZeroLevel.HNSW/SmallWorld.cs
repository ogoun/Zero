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
        private VectorSet<TItem> _vectors;
        private Layer<TItem>[] _layers;
        private int EntryPoint = 0;
        private int MaxLayer = 0;
        private readonly ProbabilityLayerNumberGenerator _layerLevelGenerator;
        private ReaderWriterLockSlim _lockGraph = new ReaderWriterLockSlim();

        public readonly Func<int, int, float> DistanceFunction;
        public TItem GetVector(int id) => _vectors[id];
        public IDictionary<int, HashSet<int>> GetLinks() => _layers[0].Links;

        public SmallWorld(NSWOptions<TItem> options)
        {
            _options = options;
            _vectors = new VectorSet<TItem>();
            _layers = new Layer<TItem>[_options.LayersCount];
            _layerLevelGenerator = new ProbabilityLayerNumberGenerator(_options.LayersCount, _options.M);

            DistanceFunction = new Func<int, int, float>((id1, id2) => _options.Distance(_vectors[id1], _vectors[id2]));

            for (int i = 0; i < _options.LayersCount; i++)
            {
                _layers[i] = new Layer<TItem>(_options, _vectors, i == 0);
            }
        }

        public SmallWorld(NSWOptions<TItem> options, Stream stream)
        {
            _options = options;
            _layerLevelGenerator = new ProbabilityLayerNumberGenerator(_options.LayersCount, _options.M);
            DistanceFunction = new Func<int, int, float>((id1, id2) => _options.Distance(_vectors[id1], _vectors[id2]));
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

        /*
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
        */

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
            var W = new MinHeap(_options.EFConstruction + 1);
            // ep ← get enter point for hnsw
            var ep = _layers[MaxLayer].FindEntryPointAtLayer(distance);
            if (ep == -1)
                ep = EntryPoint;

            var epDist = distance(ep);
            // L ← level of ep // top layer for hnsw
            var L = MaxLayer;
            // l ← ⌊-ln(unif(0..1))∙mL⌋ // new element’s level            
            int l = _layerLevelGenerator.GetRandomLayer();

            // Проход с верхнего уровня до уровня где появляется элемент, для нахождения точки входа
            int id;
            float value;
            // for lc ← L … l+1
            for (int lc = L; lc > l; --lc)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                foreach (var i in _layers[lc].KNearestAtLayer(ep, distance, 1))
                {
                    W.Push(i);
                }
                // ep ← get the nearest element from W to q
                if (W.TryPeek(out id, out value))
                {
                    ep = id;
                    epDist = value;
                }
                W.Clear();
            }
            //for lc ← min(L, l) … 0
            // connecting new node to the small world
            for (int lc = Math.Min(L, l); lc >= 0; --lc)
            {
                _layers[lc].Push(q, ep, W, distance);
                // ep ← W
                if (W.TryPeek(out id, out value))
                {
                    ep = id;
                    epDist = value;
                }
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

        public void TestWorld()
        {
            for (var v = 0; v < _vectors.Count; v++)
            {
                var nearest = _layers[0][v].ToArray();
                if (nearest.Length > _layers[0].M)
                {
                    Console.WriteLine($"V{v}. Count of links ({nearest.Length}) more than max ({_layers[0].M})");
                }
            }
            // coverage test
            var ep = 0;
            var visited = new HashSet<int>();
            var next = new Stack<int>();
            next.Push(ep);
            while (next.Count > 0)
            {
                ep = next.Pop();
                visited.Add(ep);
                foreach (var n in _layers[0].GetNeighbors(ep))
                {
                    if (visited.Contains(n) == false)
                    {
                        next.Push(n);
                    }
                }
            }
            if (visited.Count != _vectors.Count)
            {
                Console.Write($"Vectors count ({_vectors.Count}) less than BFS visited nodes count ({visited.Count})");
            }
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

                int id;
                float value;
                var distance = new Func<int, float>(candidate => _options.Distance(q, _vectors[candidate]));

                // W ← ∅ // set for the current nearest elements
                var W = new MinHeap(k + 1);
                // ep ← get enter point for hnsw
                var ep = EntryPoint;
                // L ← level of ep // top layer for hnsw
                var L = MaxLayer;
                // for lc ← L … 1
                for (int layer = L; layer > 0; --layer)
                {
                    // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                    foreach (var i in _layers[layer].KNearestAtLayer(ep, distance, 1))
                    {
                        W.Push(i);
                    }
                    // ep ← get nearest element from W to q
                    if (W.TryPeek(out id, out value))
                    {
                        ep = id;
                    }
                    W.Clear();
                }
                // W ← SEARCH-LAYER(q, ep, ef, lc =0)
                foreach (var i in _layers[0].KNearestAtLayer(ep, distance, k))
                {
                    W.Push(i);
                }
                // return K nearest elements from W to q
                return W;
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

                int id;
                float value;
                var distance = new Func<int, float>(candidate => _options.Distance(q, _vectors[candidate]));

                // W ← ∅ // set for the current nearest elements
                var W = new MinHeap(k + 1);
                // ep ← get enter point for hnsw
                var ep = EntryPoint;
                // L ← level of ep // top layer for hnsw
                var L = MaxLayer;
                // for lc ← L … 1
                for (int layer = L; layer > 0; --layer)
                {
                    // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                    foreach (var i in _layers[layer].KNearestAtLayer(ep, distance, 1))
                    {
                        W.Push(i);
                    }
                    // ep ← get nearest element from W to q
                    if (W.TryPeek(out id, out value))
                    {
                        ep = id;
                    }
                    W.Clear();
                }
                // W ← SEARCH-LAYER(q, ep, ef, lc =0)
                foreach (var i in _layers[0].KNearestAtLayer(ep, distance, k, context))
                {
                    W.Push(i);
                }
                // return K nearest elements from W to q
                return W;
            }
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }
        

        /*
        private IEnumerable<(int, float)> KNearest(int k, SearchContext context)
        {
            _lockGraph.EnterReadLock();
            try
            {
                if (_vectors.Count == 0)
                {
                    return Enumerable.Empty<(int, float)>();
                }
                // W ← ∅ // set for the current nearest elements
                var W = new MinHeap(k + 1);
                // W ← SEARCH-LAYER(q, ep, ef, lc =0)
                foreach (var i in _layers[0].KNearestAtLayer(W, k, context))
                {
                    W.Push(i);
                }
                // return K nearest elements from W to q
                return W;
            }
            finally
            {
                _lockGraph.ExitReadLock();
            }
        }
        */
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
                _vectors = new VectorSet<TItem>();
                _vectors.Deserialize(reader);
                var countLayers = reader.ReadInt32();
                _layers = new Layer<TItem>[countLayers];
                for (int i = 0; i < countLayers; i++)
                {
                    _layers[i] = new Layer<TItem>(_options, _vectors, i == 0);
                    _layers[i].Deserialize(reader);
                }
            }
        }
    }
}
