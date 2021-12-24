using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.HNSW.Services;
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
        public readonly int M;
        private readonly Dictionary<int, float> connections;
        internal IDictionary<int, HashSet<int>> Links => _links.Links;

        /// <summary>
        /// There are links е the layer
        /// </summary>
        internal bool HasLinks => (_links.Count > 0);

        internal IEnumerable<int> this[int vector_index] => _links.FindNeighbors(vector_index);

        /// <summary>
        /// HNSW layer
        /// <remarks>
        /// Article: Section 4.1:
        /// "Selection of the Mmax0 (the maximum number of connections that an element can have in the zero layer) also
        /// has a strong influence on the search performance, especially in case of high quality(high recall) search.
        /// Simulations show that setting Mmax0 to M(this corresponds to kNN graphs on each layer if the neighbors
        /// selection heuristic is not used) leads to a very strong performance penalty at high recall.
        /// Simulations also suggest that 2∙M is a good choice for Mmax0;
        /// setting the parameter higher leads to performance degradation and excessive memory usage."
        /// </remarks>
        /// </summary>
        /// <param name="options">HNSW graph options</param>
        /// <param name="vectors">General vector set</param>
        internal Layer(NSWOptions<TItem> options, VectorSet<TItem> vectors, bool nswLayer)
        {
            _options = options;
            _vectors = vectors;
            M = nswLayer ? 2 * _options.M : _options.M;
            _links = new LinksSet(M);
            connections = new Dictionary<int, float>(M + 1);
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

        internal void Push(int q, int ep, MinHeap W, Func<int, float> distance)
        {
            if (HasLinks == false)
            {
                AddBidirectionallConnections(q, q);
            }
            else
            {
                // W ← SEARCH - LAYER(q, ep, efConstruction, lc)
                foreach (var i in KNearestAtLayer(ep, distance, _options.EFConstruction))
                {
                    W.Push(i);
                }

                int count = 0;
                connections.Clear();
                while (count < M && W.Count > 0)
                {
                    var nearest = W.Pop();
                    var nearest_nearest = GetNeighbors(nearest.Item1).ToArray();
                    if (nearest_nearest.Length < M)
                    {
                        if (AddBidirectionallConnections(q, nearest.Item1))
                        {
                            connections.Add(nearest.Item1, nearest.Item2);
                            count++;
                        }
                    }
                    else
                    {
                        if ((M - count) < 2)
                        {
                            // remove link q - max_q
                            var max = connections.OrderBy(pair => pair.Value).First();
                            RemoveBidirectionallConnections(q, max.Key);
                            connections.Remove(max.Key);
                        }
                        // get nearest_nearest candidate
                        var mn_id = -1;
                        var mn_d = float.MinValue;
                        for (int i = 0; i < nearest_nearest.Length; i++)
                        {
                            var d = _options.Distance(_vectors[nearest.Item1], _vectors[nearest_nearest[i]]);
                            if (q != nearest_nearest[i] && connections.ContainsKey(nearest_nearest[i]) == false)
                            {
                                if (mn_id == -1 || d > mn_d)
                                {
                                    mn_d = d;
                                    mn_id = nearest_nearest[i];
                                }
                            }
                        }
                        // remove link neareset - nearest_nearest
                        RemoveBidirectionallConnections(nearest.Item1, mn_id);
                        // add link q - neareset
                        if (AddBidirectionallConnections(q, nearest.Item1))
                        {
                            connections.Add(nearest.Item1, nearest.Item2);
                            count++;
                        }
                        // add link q - max_nearest_nearest
                        if (AddBidirectionallConnections(q, mn_id))
                        {
                            connections.Add(mn_id, mn_d);
                            count++;
                        }
                    }
                }
            }
        }

        internal void RemoveBidirectionallConnections(int q, int p)
        {
            _links.RemoveIndex(q, p);
        }

        internal bool AddBidirectionallConnections(int q, int p)
        {
            if (q == p)
            {
                if (EntryPoint >= 0)
                {
                    return _links.Add(q, EntryPoint);
                }
                else
                {
                    EntryPoint = q;
                }
            }
            else
            {
                return _links.Add(q, p);
            }
            return false;
        }

        private int EntryPoint = -1;

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

        internal IEnumerable<int> GetNeighbors(int id) => _links.FindNeighbors(id);

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
