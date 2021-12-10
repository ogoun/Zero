using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// NSW graph
    /// </summary>
    internal sealed class ReadOnlyLayer<TItem>
        : IBinarySerializable
    {
        private readonly ReadOnlyVectorSet<TItem> _vectors;
        private readonly ReadOnlyCompactBiDirectionalLinksSet _links;

        /// <summary>
        /// HNSW layer
        /// </summary>
        /// <param name="vectors">General vector set</param>
        internal ReadOnlyLayer(ReadOnlyVectorSet<TItem> vectors)
        {
            _vectors = vectors;
            _links = new ReadOnlyCompactBiDirectionalLinksSet();
        }

        #region Implementation of https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf
        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        internal void KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, IDictionary<int, float> W, int ef)
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
            var v = new VisitedBitSet(_vectors.Count, 1);
            // v ← ep // set of visited elements
            v.Add(entryPointId);
            // C ← ep // set of candidates
            var C = new Dictionary<int, float>();
            C.Add(entryPointId, targetCosts(entryPointId));
            // W ← ep // dynamic list of found nearest neighbors
            W.Add(entryPointId, C[entryPointId]);

            var popCandidate = new Func<(int, float)>(() => { var pair = C.OrderBy(e => e.Value).First(); C.Remove(pair.Key); return (pair.Key, pair.Value); });
            var fartherFromResult = new Func<(int, float)>(() => { var pair = W.OrderByDescending(e => e.Value).First(); return (pair.Key, pair.Value); });
            var fartherPopFromResult = new Action(() => { var pair = W.OrderByDescending(e => e.Value).First(); W.Remove(pair.Key); });
            // run bfs
            while (C.Count > 0)
            {
                // get next candidate to check and expand
                var toExpand = popCandidate();
                var farthestResult = fartherFromResult();
                if (toExpand.Item2 > farthestResult.Item2)
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
                        // enqueue perspective neighbours to expansion list
                        farthestResult = fartherFromResult();

                        var neighbourDistance = targetCosts(neighbourId);
                        if (W.Count < ef || neighbourDistance < farthestResult.Item2)
                        {
                            C.Add(neighbourId, neighbourDistance);
                            W.Add(neighbourId, neighbourDistance);
                            if (W.Count > ef)
                            {
                                fartherPopFromResult();
                            }
                        }
                        v.Add(neighbourId);
                    }
                }
            }
            C.Clear();
            v.Clear();
        }

        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        internal void KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, IDictionary<int, float> W, int ef, SearchContext context)
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
            var v = new VisitedBitSet(_vectors.Count, 1);
            // v ← ep // set of visited elements
            v.Add(entryPointId);
            // C ← ep // set of candidates
            var C = new Dictionary<int, float>();
            C.Add(entryPointId, targetCosts(entryPointId));
            // W ← ep // dynamic list of found nearest neighbors
            if (context.IsActiveNode(entryPointId))
            {
                W.Add(entryPointId, C[entryPointId]);
            }
            var popCandidate = new Func<(int, float)>(() => { var pair = C.OrderBy(e => e.Value).First(); C.Remove(pair.Key); return (pair.Key, pair.Value); });
            var farthestDistance = new Func<float>(() => { var pair = W.OrderByDescending(e => e.Value).First(); return pair.Value; });
            var fartherPopFromResult = new Action(() => { var pair = W.OrderByDescending(e => e.Value).First(); W.Remove(pair.Key); });
            // run bfs
            while (C.Count > 0)
            {
                // get next candidate to check and expand
                var toExpand = popCandidate();
                if (W.Count > 0)
                {
                    if (toExpand.Item2 > farthestDistance())
                    {
                        // the closest candidate is farther than farthest result
                        break;
                    }
                }

                // expand candidate
                var neighboursIds = GetNeighbors(toExpand.Item1).ToArray();
                for (int i = 0; i < neighboursIds.Length; ++i)
                {
                    int neighbourId = neighboursIds[i];
                    if (!v.Contains(neighbourId))
                    {
                        // enqueue perspective neighbours to expansion list
                        var neighbourDistance = targetCosts(neighbourId);
                        if (context.IsActiveNode(neighbourId))
                        {
                            if (W.Count < ef || (W.Count > 0 && neighbourDistance < farthestDistance()))
                            {
                                W.Add(neighbourId, neighbourDistance);
                                if (W.Count > ef)
                                {
                                    fartherPopFromResult();
                                }
                            }
                        }
                        if (W.Count < ef)
                        {
                            C.Add(neighbourId, neighbourDistance);
                        }
                        v.Add(neighbourId);
                    }
                }
            }
            C.Clear();
            v.Clear();
        }
        #endregion

        private IEnumerable<int> GetNeighbors(int id) => _links.FindLinksForId(id);

        public void Serialize(IBinaryWriter writer)
        {
            _links.Serialize(writer);
        }

        public void Deserialize(IBinaryReader reader)
        {
            _links.Deserialize(reader);
        }
    }
}
