using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW
{
    public class SmallWorld<TItem>
    {
        private readonly NSWOptions<TItem> _options;
        private readonly VectorSet<TItem> _vectors;
        private readonly Layer<TItem>[] _layers;

        private Layer<TItem> EnterPointsLayer => _layers[_layers.Length - 1];
        private Layer<TItem> LastLayer => _layers[0];

        public SmallWorld(NSWOptions<TItem> options)
        {
            _options = options;
            _vectors = new VectorSet<TItem>();
            _layers = new Layer<TItem>[_options.LayersCount];
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
            var insert = vectors.ToArray();
            var ids = new int[insert.Length];
            for (int i = 0; i < insert.Length; i++)
            {
                var item = insert[i];
                ids[i] = Insert(item);
            }
            return ids;
        }

        public int Insert(TItem item)
        {
            var id = _vectors.Append(item);
            INSERT(id);
            return id;
        }

        #region https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf
        /// <summary>
        /// Algorithm 1
        /// </summary>
        /// <param name="q">new element</param>
        public void INSERT(int q)
        {
            // W ← ∅ // list for the currently found nearest elements
            IDictionary<int, float> W;
            // ep ← get enter point for hnsw
            var ep = EnterPointsLayer.GetEntryPointFor(q);
            // L ← level of ep // top layer for hnsw
            var L = _layers.Length - 1;
            // l ← ⌊-ln(unif(0..1))∙mL⌋ // new element’s level            
            int l = DefaultRandomGenerator.Instance.Next(0, _options.LayersCount - 1);
            // for lc ← L … l+1
            for (int lc = L; lc > l; lc--)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                W = _layers[lc].SEARCH_LAYER(q, ep, 1);
                // ep ← get the nearest element from W to q
                ep = W.OrderBy(p => p.Value).First().Key;
            }
            //for lc ← min(L, l) … 0
            for (int lc = Math.Min(L, l); lc >= 0; lc--)
            {
                // W ← SEARCH - LAYER(q, ep, efConstruction, lc)
                W = _layers[lc].SEARCH_LAYER(q, ep, _options.EFConstruction);
                // neighbors ← SELECT-NEIGHBORS(q, W, M, lc) // alg. 3 or alg. 4
                var neighbors = _layers[lc].SELECT_NEIGHBORS_SIMPLE(q, W);
                // add bidirectionall connectionts from neighbors to q at layer lc
                // for each e ∈ neighbors // shrink connections if needed
                foreach (var e in neighbors)
                {
                    // eConn ← neighbourhood(e) at layer lc
                    _layers[lc].AddBidirectionallConnectionts(q, e.Key, e.Value);
                }
                // ep ← W
                ep = W.OrderBy(p => p.Value).First().Key;
            }
            //  if l > L
            //      set enter point for hnsw to q
        }

        /// <summary>
        /// Algorithm 5
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="K">number of nearest neighbors to return</param>
        /// <returns>: K nearest elements to q</returns>
        public IList<int> K_NN_SEARCH(int q, int K)
        {
            // W ← ∅ // set for the current nearest elements
            IDictionary<int, float> W;
            // ep ← get enter point for hnsw
            var ep = EnterPointsLayer.GetEntryPointFor(q);
            //  L ← level of ep // top layer for hnsw
            var L = _options.LayersCount - 1;
            // for lc ← L … 1
            for (var lc = L; lc > 0; lc--)
            {
                // W ← SEARCH-LAYER(q, ep, ef = 1, lc)
                W = _layers[lc].SEARCH_LAYER(q, ep, 1);
                // ep ← get nearest element from W to q
                ep = W.OrderBy(p => p.Value).First().Key;
            }
            // W ← SEARCH-LAYER(q, ep, ef, lc =0)
            W = LastLayer.SEARCH_LAYER(q, ep, _options.EF);
            // return K nearest elements from W to q
            return W.OrderBy(p => p.Value).Take(K).Select(p => p.Key).ToList();
        }
        #endregion
    }
}
