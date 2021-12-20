using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.HNSW.Services;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// NSW graph
    /// </summary>
    internal sealed class Layer<TItem>
        : IBinarySerializable
    {
        private readonly NSWOptions<TItem> _options;
        private readonly VectorSet<TItem> _vectors;
        private readonly LinksSet _links;
        //internal SortedList<long, float> Links => _links.Links;

        /// <summary>
        /// There are links е the layer
        /// </summary>
        internal bool HasLinks => (_links.Count > 0);

        private int GetM(bool nswLayer)
        {
            return nswLayer ? 2 * _options.M : _options.M;
        }

        /// <summary>
        /// HNSW layer
        /// </summary>
        /// <param name="options">HNSW graph options</param>
        /// <param name="vectors">General vector set</param>
        internal Layer(NSWOptions<TItem> options, VectorSet<TItem> vectors, bool nswLayer)
        {
            _options = options;
            _vectors = vectors;
            _links = new LinksSet(GetM(nswLayer), (id1, id2) => options.Distance(_vectors[id1], _vectors[id2]));
        }

        internal int FindEntryPointAtLayer(Func<int, float> targetCosts)
        {
            if (_links.Count == 0) return EntryPoint;
            var set = new HashSet<int>(_links.Items().Select(p => p.Item1));
            int minId = -1;
            float minDist = float.MaxValue;
            foreach (var id in set)
            {
                var d = targetCosts(id);
                if (d < minDist && Math.Abs(d) > float.Epsilon)
                {
                    minDist = d;
                    minId = id;
                }
            }
            return minId;
        }

        internal void AddBidirectionallConnections(int q, int p)
        {
            if (q == p)
            {
                if (EntryPoint >= 0)
                {
                    _links.Add(q, EntryPoint);
                }
                else
                {
                    EntryPoint = q;
                }
            }
            else
            {
                _links.Add(q, p);
            }
        }

        private int EntryPoint = -1;

        internal void Trim(int id) => _links.Trim(id);

        #region Implementation of https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf
        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        internal IEnumerable<(int, float)> KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, int ef)
        {
            /*
             * v ← ep // set of visited elements
             * C ← ep // set of candidates
             * W ← ep // dynamic list of found nearest neighbors
             * while │C│ > 0
             *   c ← extract nearest element from C to q
             *   f ← get furthest element from W to q
             *   if distance(c, q) > distance(f, q)
             *     break // all elements in W are evaluated
             *   for each e ∈ neighbourhood(c) at layer lc // update C and W
             *     if e ∉ v
             *       v ← v ⋃ e
             *       f ← get furthest element from W to q
             *       if distance(e, q) < distance(f, q) or │W│ < ef
             *         C ← C ⋃ e
             *         W ← W ⋃ e
             *         if │W│ > ef
             *           remove furthest element from W to q
             * return W
             */

            int farthestId;
            float farthestDistance;
            var d = targetCosts(entryPointId);

            var v = new VisitedBitSet(_vectors.Count, _options.M);
            // * v ← ep // set of visited elements
            v.Add(entryPointId);
            // * C ← ep // set of candidates
            var C = new MinHeap(ef);
            C.Push((entryPointId, d));
            // * W ← ep // dynamic list of found nearest neighbors
            var W = new MaxHeap(ef + 1);
            W.Push((entryPointId, d));

            // * while │C│ > 0
            while (C.Count > 0)
            {
                // * c ← extract nearest element from C to q
                var c = C.Pop();
                // * f ← get furthest element from W to q
                // * if distance(c, q) > distance(f, q)
                if (W.TryPeek(out _, out farthestDistance) && c.Item2 > farthestDistance)
                {
                    // * break // all elements in W are evaluated
                    break;
                }

                // * for each e ∈ neighbourhood(c) at layer lc // update C and W
                foreach (var e in GetNeighbors(c.Item1))
                {
                    // * if e ∉ v
                    if (!v.Contains(e))
                    {
                        // * v ← v ⋃ e
                        v.Add(e);
                        // * f ← get furthest element from W to q
                        W.TryPeek(out farthestId, out farthestDistance);

                        var eDistance = targetCosts(e);
                        // * if distance(e, q) < distance(f, q) or │W│ < ef
                        if (W.Count < ef || (farthestId >= 0 && eDistance < farthestDistance))
                        {
                            // * C ← C ⋃ e
                            C.Push((e, eDistance));
                            // * W ← W ⋃ e
                            W.Push((e, eDistance));
                            // * if │W│ > ef
                            if (W.Count > ef)
                            {
                                // * remove furthest element from W to q
                                W.Pop();
                            }
                        }
                    }
                }
            }
            C.Clear();
            v.Clear();
            return W;
        }

        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        /*
        internal IEnumerable<(int, float)> KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, int ef, SearchContext context)
        {
            int farthestId;
            float farthestDistance;
            var d = targetCosts(entryPointId);

            var v = new VisitedBitSet(_vectors.Count, _options.M);
            // v ← ep // set of visited elements
            v.Add(entryPointId);
            // C ← ep // set of candidates
            var C = new MinHeap(ef);
            C.Push((entryPointId, d));
            // W ← ep // dynamic list of found nearest neighbors
            var W = new MaxHeap(ef + 1);
            // W ← ep // dynamic list of found nearest neighbors
            if (context.IsActiveNode(entryPointId))
            {
                W.Push((entryPointId, d));
            }
            // run bfs
            while (C.Count > 0)
            {
                // get next candidate to check and expand
                var toExpand = C.Pop();
                if (W.TryPeek(out _, out farthestDistance) && toExpand.Item2 > farthestDistance)
                {
                    // the closest candidate is farther than farthest result
                    break;
                }

                // expand candidate
                var neighboursIds = GetNeighbors(toExpand.Item1).ToArray();
                for (int i = 0; i < neighboursIds.Length; ++i)
                {
                    int neighbourId = neighboursIds[i];
                    if (!v.Contains(neighbourId))
                    {
                        W.TryPeek(out farthestId, out farthestDistance);
                        // enqueue perspective neighbours to expansion list
                        var neighbourDistance = targetCosts(neighbourId);
                        if (context.IsActiveNode(neighbourId))
                        {
                            if (W.Count < ef || (farthestId >= 0 && neighbourDistance < farthestDistance))
                            {
                                W.Push((neighbourId, neighbourDistance));
                                if (W.Count > ef)
                                {
                                    W.Pop();
                                }
                            }
                        }
                        if (W.TryPeek(out _, out farthestDistance) && neighbourDistance < farthestDistance)
                        {
                            C.Push((neighbourId, neighbourDistance));
                        }
                        v.Add(neighbourId);
                    }
                }
            }
            C.Clear();
            v.Clear();
            return W;
        }
        */

        /// <summary>
        /// Algorithm 2, modified for LookAlike
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        /*
        internal IEnumerable<(int, float)> KNearestAtLayer(IEnumerable<(int, float)> w, int ef, SearchContext context)
        {
            // v ← ep // set of visited elements
            var v = new VisitedBitSet(_vectors.Count, _options.M);
            // C ← ep // set of candidates
            var C = new MinHeap(ef);
            foreach (var ep in context.EntryPoints)
            {
                var neighboursIds = GetNeighbors(ep).ToArray();
                for (int i = 0; i < neighboursIds.Length; ++i)
                {
                    C.Push((ep, _links.Distance(ep, neighboursIds[i])));
                }
                v.Add(ep);
            }
            // W ← ep // dynamic list of found nearest neighbors
            var W = new MaxHeap(ef + 1);
            foreach (var i in w) W.Push(i);

            // run bfs
            while (C.Count > 0)
            {
                // get next candidate to check and expand
                var toExpand = C.Pop();
                if (W.Count > 0)
                {
                    if (W.TryPeek(out _, out var dist) && toExpand.Item2 > dist)
                    {
                        // the closest candidate is farther than farthest result
                        break;
                    }
                }
                if (context.IsActiveNode(toExpand.Item1))
                {
                    if (W.Count < ef || W.Count == 0 || (W.Count > 0 && (W.TryPeek(out _, out var dist) && toExpand.Item2 < dist)))
                    {
                        W.Push((toExpand.Item1, toExpand.Item2));
                        if (W.Count > ef)
                        {
                            W.Pop();
                        }
                    }
                }
            }
            if (W.Count > ef)
            {
                while (W.Count > ef)
                {
                    W.Pop();
                }
                return W;
            }
            else
            {
                foreach (var c in W)
                {
                    C.Push((c.Item1, c.Item2));
                }
            }
            while (C.Count > 0)
            {
                // get next candidate to check and expand
                var toExpand = C.Pop();
                // expand candidate
                var neighboursIds = GetNeighbors(toExpand.Item1).ToArray();
                for (int i = 0; i < neighboursIds.Length; ++i)
                {
                    int neighbourId = neighboursIds[i];
                    if (!v.Contains(neighbourId))
                    {
                        // enqueue perspective neighbours to expansion list
                        var neighbourDistance = _links.Distance(toExpand.Item1, neighbourId);
                        if (context.IsActiveNode(neighbourId))
                        {
                            if (W.Count < ef || (W.Count > 0 && (W.TryPeek(out _, out var dist) && neighbourDistance < dist)))
                            {
                                W.Push((neighbourId, neighbourDistance));
                                if (W.Count > ef)
                                {
                                    W.Pop();
                                }
                            }
                        }
                        if (W.Count < ef)
                        {
                            C.Push((neighbourId, neighbourDistance));
                        }
                        v.Add(neighbourId);
                    }
                }
            }
            C.Clear();
            v.Clear();
            return W;
        }
        */
        #endregion

        private IEnumerable<int> GetNeighbors(int id) => _links.FindNeighbors(id);

        public void Serialize(IBinaryWriter writer)
        {
            _links.Serialize(writer);
        }

        public void Deserialize(IBinaryReader reader)
        {
            _links.Deserialize(reader);
        }

        // internal Histogram GetHistogram(HistogramMode mode) => _links.CalculateHistogram(mode);
    }
}
