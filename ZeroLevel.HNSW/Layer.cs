using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// NSW graph
    /// </summary>
    internal sealed class Layer<TItem>
    {
        private readonly NSWOptions<TItem> _options;
        private readonly VectorSet<TItem> _vectors;
        private CompactBiDirectionalLinksSet _links = new CompactBiDirectionalLinksSet();

        public Layer(NSWOptions<TItem> options, VectorSet<TItem> vectors)
        {
            _options = options;
            _vectors = vectors;
        }

        public void AddBidirectionallConnectionts(int q, int p, float qpDistance)
        {
            // поиск в ширину ближайших узлов к найденному
            var nearest = _links.FindLinksForId(p).ToArray();
            // если у найденного узла максимальное количество связей
            // if │eConn│ > Mmax // shrink connections of e
            if (nearest.Length >= _options.M)
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

        public int GetEntryPointFor(int q)
        {
            var randomLinkId = DefaultRandomGenerator.Instance.Next(0, _links.Count);
            var entryId = _links[randomLinkId].Item1;
            var v = new VisitedBitSet(_vectors._set.Count, _options.M);
            // v ← ep // set of visited elements
            var (ep, ed) = DFS_SearchMinFrom(entryId, q, v);
            return ep;
        }

        private (int, float) DFS_SearchMinFrom(int entryId, int id, VisitedBitSet visited)
        {
            visited.Add(entryId);
            int candidate = entryId;
            var candidateDistance = _options.Distance(_vectors[entryId], _vectors[id]);
            int counter = 0;
            do
            {
                var (mid, dist) = GetMinNearest(visited, entryId, candidate, candidateDistance);
                if (dist > candidateDistance)
                {
                    break;
                }
                candidate = mid;
                candidateDistance = dist;

                counter++;
            } while (counter < _options.EFConstruction);
            return (candidate, candidateDistance);
        }

        private (int, float) GetMinNearest(VisitedBitSet visited, int entryId, int id, float entryDistance)
        {
            var minId = entryId;
            var minDist = entryDistance;
            foreach (var candidate in _links.FindLinksForId(entryId).Select(l => l.Item2))
            {
                if (visited.Contains(candidate) == false)
                {
                    var dist = _options.Distance(_vectors[candidate], _vectors[id]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minId = candidate;
                    }
                    visited.Add(candidate);
                }
            }
            return (minId, minDist);
        }

        #region Implementation of https://arxiv.org/ftp/arxiv/papers/1603/1603.09320.pdf

        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        public IDictionary<int, float> SEARCH_LAYER(int q, int ep, int ef)
        {
            var v = new VisitedBitSet(_vectors._set.Count, _options.M);
            // v ← ep // set of visited elements
            v.Add(ep);
            // C ← ep // set of candidates
            var C = new Dictionary<int, float>();
            C.Add(ep, _options.Distance(_vectors[ep], _vectors[q]));
            // W ← ep // dynamic list of found nearest neighbors
            var W = new Dictionary<int, float>();
            W.Add(ep, C[ep]);
            // while │C│ > 0
            while (C.Count > 0)
            {
                // c ← extract nearest element from C to q
                var nearest = W.OrderBy(p => p.Value).First();
                var c = nearest.Key;
                var md = nearest.Value;
                // var (c, md) = GetMinimalDistanceIndex(C, q);
                C.Remove(c);
                // f ← get furthest element from W to q
                var f = W.OrderBy(p => p.Value).First().Key;
                //var f = GetMaximalDistanceIndex(W, q);
                // if distance(c, q) > distance(f, q)
                if (_options.Distance(_vectors[c], _vectors[q]) > _options.Distance(_vectors[f], _vectors[q]))
                {
                    // break // all elements in W are evaluated
                    break;
                }
                // for each e ∈ neighbourhood(c) at layer lc // update C and W
                foreach (var l in _links.FindLinksForId(c))
                {
                    var e = l.Item2;
                    // if e ∉ v
                    if (v.Contains(e) == false)
                    {
                        // v ← v ⋃ e
                        v.Add(e);
                        // f ← get furthest element from W to q
                        f = W.OrderByDescending(p => p.Value).First().Key;
                        //f = GetMaximalDistanceIndex(W, q);
                        // if distance(e, q) < distance(f, q) or │W│ < ef
                        var ed = _options.Distance(_vectors[e], _vectors[q]);
                        if (ed > _options.Distance(_vectors[f], _vectors[q])
                            || W.Count < ef)
                        {
                            // C ← C ⋃ e
                            C.Add(e, ed);
                            // W ← W ⋃ e
                            W.Add(e, ed);
                            // if │W│ > ef
                            if (W.Count > ef)
                            {
                                // remove furthest element from W to q
                                f = W.OrderByDescending(p => p.Value).First().Key;
                                //f = GetMaximalDistanceIndex(W, q);
                                W.Remove(f);
                            }
                        }
                    }
                }
            }
            //  return W
            return W;
        }

        /// <summary>
        /// Algorithm 3
        /// </summary>
        /// <param name="q">base element</param>
        /// <param name="C">candidate elements</param>
        /// <returns>Output: M nearest elements to q</returns>
        public IDictionary<int, float> SELECT_NEIGHBORS_SIMPLE(int q, IDictionary<int, float> C)
        {
            if (C.Count <= _options.M)
            {
                return new Dictionary<int, float>(C);
            }
            var output = new Dictionary<int, float>();
            // return M nearest elements from C to q
            return new Dictionary<int, float>(C.OrderBy(p => p.Value).Take(_options.M));
        }

        /// <summary>
        /// Algorithm 4
        /// </summary>
        /// <param name="q">base element</param>
        /// <param name="C">candidate elements</param>
        /// <param name="extendCandidates">flag indicating whether or not to extend candidate list</param>
        /// <param name="keepPrunedConnections">flag indicating whether or not to add discarded elements</param>
        /// <returns>Output: M elements selected by the heuristic</returns>
        public IDictionary<int, float> SELECT_NEIGHBORS_HEURISTIC(int q, IDictionary<int, float> C, bool extendCandidates, bool keepPrunedConnections)
        {
            // R ← ∅
            var R = new Dictionary<int, float>();
            // W ← C // working queue for the candidates
            var W = new List<int>(C.Select(p => p.Key));
            // if extendCandidates // extend candidates by their neighbors
            if (extendCandidates)
            {
                // for each e ∈ C
                foreach (var e in C)
                {
                    // for each e_adj ∈ neighbourhood(e) at layer lc
                    foreach (var l in _links.FindLinksForId(e.Key))
                    {
                        var e_adj = l.Item2;
                        // if eadj ∉ W
                        if (W.Contains(e_adj) == false)
                        {
                            // W ← W ⋃ eadj
                            W.Add(e_adj);
                        }
                    }
                }
            }
            //  Wd ← ∅ // queue for the discarded candidates
            var Wd = new Dictionary<int, float>();
            // while │W│ > 0 and │R│< M
            while (W.Count > 0 && R.Count < _options.M)
            {
                // e ← extract nearest element from W to q
                var (e, ed) = GetMinimalDistanceIndex(W, q);
                W.Remove(e);
                // if e is closer to q compared to any element from R
                if (ed < R.Min(pair => pair.Value))
                {
                    // R ← R ⋃ e
                    R.Add(e, ed);
                }
                // else
                {
                    // Wd ← Wd ⋃ e
                    Wd.Add(e, ed);
                }
                // if keepPrunedConnections // add some of the discarded // connections from Wd
                if (keepPrunedConnections)
                {
                    // while │Wd│> 0 and │R│< M
                    while (Wd.Count > 0 && R.Count < _options.M)
                    {
                        // R ← R ⋃ extract nearest element from Wd to q
                        var nearest = Wd.Aggregate((l, r) => l.Value < r.Value ? l : r);
                        Wd.Remove(nearest.Key);
                        R.Add(nearest.Key, nearest.Value);
                    }
                }
            }
            //  return R
            return R;
        }


        #endregion

        
        private (int, float) GetMinimalDistanceIndex(IList<int> self, int q)
        {
            float min = _options.Distance(_vectors[self[0]], _vectors[q]);
            int minIndex = 0;
            for (int i = 1; i < self.Count; ++i)
            {
                var dist = _options.Distance(_vectors[self[i]], _vectors[q]);
                if (dist < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }
            return (minIndex, min);
        }
    }
}
