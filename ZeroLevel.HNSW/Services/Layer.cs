using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly CompactBiDirectionalLinksSet _links;

        /// <summary>
        /// There are links е the layer
        /// </summary>
        internal bool HasLinks => (_links.Count > 0);

        /// <summary>
        /// HNSW layer
        /// </summary>
        /// <param name="options">HNSW graph options</param>
        /// <param name="vectors">General vector set</param>
        internal Layer(NSWOptions<TItem> options, VectorSet<TItem> vectors)
        {
            _options = options;
            _vectors = vectors;
            _links = new CompactBiDirectionalLinksSet();
        }

        /// <summary>
        /// Adding new bidirectional link
        /// </summary>
        /// <param name="q">New node</param>
        /// <param name="p">The node with which the connection will be made</param>
        /// <param name="qpDistance"></param>
        /// <param name="isMapLayer"></param>
        internal void AddBidirectionallConnections(int q, int p, float qpDistance, bool isMapLayer)
        {
            // поиск в ширину ближайших узлов к найденному
            var nearest = _links.FindLinksForId(p).ToArray();
            // если у найденного узла максимальное количество связей
            // if │eConn│ > Mmax // shrink connections of e
            if (nearest.Length >= (isMapLayer ? _options.M * 2 : _options.M))
            {
                // ищем связь с самой большой дистанцией
                float distance = nearest[0].Item3;
                int index = 0;
                for (int ni = 1; ni < nearest.Length; ni++)
                {
                    if (nearest[ni].Item3 > distance)
                    {
                        index = ni;
                        distance = nearest[ni].Item3;
                    }
                }
                // делаем перелинковку вставляя новый узел между найденными
                var id1 = nearest[index].Item1;
                var id2 = nearest[index].Item2;
                _links.Relink(id1, id2, q, qpDistance, _options.Distance(_vectors[id2], _vectors[q]));
            }
            else
            {
                // добавляем связь нового узла к найденному
                _links.Add(q, p, qpDistance);
            }
        }

        /// <summary>
        /// Adding a node with a connection to itself
        /// </summary>
        /// <param name="q"></param>
        internal void Append(int q)
        {
            _links.Add(q, q, 0);
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
            var v = new VisitedBitSet(_vectors.Count, _options.M);
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
        internal void KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, IDictionary<int, float> W, int ef, HashSet<int> activeNodes)
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
            var v = new VisitedBitSet(_vectors.Count, _options.M);
            // v ← ep // set of visited elements
            v.Add(entryPointId);
            // C ← ep // set of candidates
            var C = new Dictionary<int, float>();
            C.Add(entryPointId, targetCosts(entryPointId));
            // W ← ep // dynamic list of found nearest neighbors
            if (activeNodes.Contains(entryPointId))
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
                        if (activeNodes.Contains(neighbourId))
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

        /// <summary>
        /// Algorithm 3
        /// </summary>
        internal IDictionary<int, float> SELECT_NEIGHBORS_SIMPLE(Func<int, float> distance, IDictionary<int, float> candidates, int M)
        {
            var bestN = M;
            var W = new Dictionary<int, float>(candidates);
            if (W.Count > bestN)
            {
                var popFarther = new Action(() => { var pair = W.OrderByDescending(e => e.Value).First(); W.Remove(pair.Key); });
                while (W.Count > bestN)
                {
                    popFarther();
                }
            }
            // return M nearest elements from C to q
            return W;
        }



        /// <summary>
        /// Algorithm 4
        /// </summary>
        /// <param name="q">base element</param>
        /// <param name="C">candidate elements</param>
        /// <param name="extendCandidates">flag indicating whether or not to extend candidate list</param>
        /// <param name="keepPrunedConnections">flag indicating whether or not to add discarded elements</param>
        /// <returns>Output: M elements selected by the heuristic</returns>
        internal IDictionary<int, float> SELECT_NEIGHBORS_HEURISTIC(Func<int, float> distance, IDictionary<int, float> candidates, int M)
        {
            // R ← ∅
            var R = new Dictionary<int, float>();
            // W ← C // working queue for the candidates
            var W = new Dictionary<int, float>(candidates);
            // if extendCandidates // extend candidates by their neighbors
            if (_options.ExpandBestSelection)
            {
                var extendBuffer = new HashSet<int>();
                // for each e ∈ C
                foreach (var e in W)
                {
                    var neighbors = GetNeighbors(e.Key);
                    // for each e_adj ∈ neighbourhood(e) at layer lc
                    foreach (var e_adj in neighbors)
                    {
                        // if eadj ∉ W
                        if (extendBuffer.Contains(e_adj) == false)
                        {
                            extendBuffer.Add(e_adj);
                        }
                    }
                }
                // W ← W ⋃ eadj
                foreach (var id in extendBuffer)
                {
                    W[id] = distance(id);
                }
            }

            //  Wd ← ∅ // queue for the discarded candidates
            var Wd = new Dictionary<int, float>();


            var popCandidate = new Func<(int, float)>(() => { var pair = W.OrderBy(e => e.Value).First(); W.Remove(pair.Key); return (pair.Key, pair.Value); });
            var fartherFromResult = new Func<(int, float)>(() => { if (R.Count == 0) return (-1, 0f); var pair = R.OrderByDescending(e => e.Value).First(); return (pair.Key, pair.Value); });
            var popNearestDiscarded = new Func<(int, float)>(() => { var pair = Wd.OrderBy(e => e.Value).First(); Wd.Remove(pair.Key); return (pair.Key, pair.Value); });


            // while │W│ > 0 and │R│< M
            while (W.Count > 0 && R.Count < M)
            {
                // e ← extract nearest element from W to q
                var (e, ed) = popCandidate();
                var (fe, fd) = fartherFromResult();

                // if e is closer to q compared to any element from R
                if (R.Count == 0 ||
                    ed < fd)
                {
                    // R ← R ⋃ e
                    R.Add(e, ed);
                }
                else
                {
                    // Wd ← Wd ⋃ e
                    Wd.Add(e, ed);
                }
            }
            // if keepPrunedConnections // add some of the discarded // connections from Wd
            if (_options.KeepPrunedConnections)
            {
                // while │Wd│> 0 and │R│< M
                while (Wd.Count > 0 && R.Count < M)
                {
                    // R ← R ⋃ extract nearest element from Wd to q
                    var nearest = popNearestDiscarded();
                    R[nearest.Item1] = nearest.Item2;
                }
            }
            //  return R
            return R;
        }
        #endregion

        private IEnumerable<int> GetNeighbors(int id) => _links.FindLinksForId(id).Select(d => d.Item2);

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