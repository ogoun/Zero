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
        private readonly CompactBiDirectionalLinksSet _links;
        internal SortedList<long, float> Links => _links.Links;

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
                    // Если осталась ссылка узла на себя, удаляем ее в первую очередь
                    if (nearest[ni].Item1 == nearest[ni].Item2)
                    {
                        index = ni;
                        break;
                    }
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
                if (nearest.Length == 1 && nearest[0].Item1 == nearest[0].Item2)
                {
                    // убираем связи на самих себя
                    var id1 = nearest[0].Item1;
                    var id2 = nearest[0].Item2;
                    _links.Relink(id1, id2, q, qpDistance, _options.Distance(_vectors[id2], _vectors[q]));
                }
                else
                {
                    // добавляем связь нового узла к найденному
                    _links.Add(q, p, qpDistance);
                }
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
        internal int FindEntryPointAtLayer(Func<int, float> targetCosts)
        {
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

        /// <summary>
        /// Algorithm 2
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        internal IEnumerable<(int, float)> KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, IEnumerable<(int, float)> w, int ef)
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
            var W = new MaxHeap(ef + 1);
            foreach (var i in w) W.Push(i);

            var d = targetCosts(entryPointId);
            // C ← ep // set of candidates
            var C = new MinHeap(ef);
            C.Push((entryPointId, d));
            // W ← ep // dynamic list of found nearest neighbors
            W.Push((entryPointId, d));

            int farthestId;
            float farthestDistance;

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
                        // enqueue perspective neighbours to expansion list
                        W.TryPeek(out farthestId, out farthestDistance);

                        var neighbourDistance = targetCosts(neighbourId);
                        if (W.Count < ef || (farthestId >= 0 && neighbourDistance < farthestDistance))
                        {
                            C.Push((neighbourId, neighbourDistance));

                            W.Push((neighbourId, neighbourDistance));
                            if (W.Count > ef)
                            {
                                W.Pop();
                            }
                        }
                        v.Add(neighbourId);
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
        internal IEnumerable<(int, float)> KNearestAtLayer(int entryPointId, Func<int, float> targetCosts, IEnumerable<(int, float)> w, int ef, SearchContext context)
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

            var W = new MaxHeap(ef + 1);
            foreach (var i in w) W.Push(i);

            // C ← ep // set of candidates
            var C = new MinHeap(ef);
            var d = targetCosts(entryPointId);
            C.Push((entryPointId, d));
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
                if (W.Count > 0)
                {
                    if(W.TryPeek(out _, out var dist ))
                    if (toExpand.Item2 > dist)
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
                            if (W.Count < ef || (W.Count > 0 && (W.TryPeek(out _, out var dist) &&  neighbourDistance < dist)))
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

        /// <summary>
        /// Algorithm 2, modified for LookAlike
        /// </summary>
        /// <param name="q">query element</param>
        /// <param name="ep">enter points ep</param>
        /// <returns>Output: ef closest neighbors to q</returns>
        internal IEnumerable<(int, float)> KNearestAtLayer(IEnumerable<(int, float)> w, int ef, SearchContext context)
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

        /// <summary>
        /// Algorithm 3
        /// </summary>
        internal MaxHeap SELECT_NEIGHBORS_SIMPLE(IEnumerable<(int, float)> w, int M)
        {
            var W = new MaxHeap(w.Count());
            foreach (var i in w) W.Push(i);
            var bestN = M;
            if (W.Count > bestN)
            {
                while (W.Count > bestN)
                {
                    W.Pop();
                }
            }
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
        internal MaxHeap SELECT_NEIGHBORS_HEURISTIC(Func<int, float> distance, IEnumerable<(int, float)> w, int M)
        {
            // R ← ∅
            var R = new MaxHeap(_options.EFConstruction);
            // W ← C // working queue for the candidates
            var W = new MaxHeap(_options.EFConstruction + 1);
            foreach (var i in w) W.Push(i);
            // if extendCandidates // extend candidates by their neighbors
            if (_options.ExpandBestSelection)
            {
                var extendBuffer = new HashSet<int>();
                // for each e ∈ C
                foreach (var e in W)
                {
                    var neighbors = GetNeighbors(e.Item1);
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
                    W.Push((id, distance(id)));
                }
            }

            //  Wd ← ∅ // queue for the discarded candidates
            var Wd = new MinHeap(_options.EFConstruction);
            // while │W│ > 0 and │R│< M
            while (W.Count > 0 && R.Count < M)
            {
                // e ← extract nearest element from W to q
                var (e, ed) = W.Pop();
                var (fe, fd) = R.Pop();

                // if e is closer to q compared to any element from R
                if (R.Count == 0 ||
                    ed < fd)
                {
                    // R ← R ⋃ e
                    R.Push((e, ed));
                }
                else
                {
                    // Wd ← Wd ⋃ e
                    Wd.Push((e, ed));
                }
            }
            // if keepPrunedConnections // add some of the discarded // connections from Wd
            if (_options.KeepPrunedConnections)
            {
                // while │Wd│> 0 and │R│< M
                while (Wd.Count > 0 && R.Count < M)
                {
                    // R ← R ⋃ extract nearest element from Wd to q
                    var nearest = Wd.Pop();
                    R.Push((nearest.Item1, nearest.Item2));
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

        internal Histogram GetHistogram(HistogramMode mode) => _links.CalculateHistogram(mode);
    }
}
