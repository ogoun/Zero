using System.Collections.Generic;

namespace ZeroLevel.HNSW
{
    public class SplittedLALGraph
    {
        private readonly IDictionary<int, LALGraph> _graphs = new Dictionary<int, LALGraph>();

        public void Append(LALGraph graph, int c)
        {
            _graphs.Add(c, graph);
        }

        public IDictionary<int, List<int>> KNearest(int k, IDictionary<int, SearchContext> contexts)
        {
            var partial_k = 1 + (k / _graphs.Count);
            var result = new Dictionary<int, List<int>>();
            int step = 1;
            foreach (var graph in _graphs)
            {
                result.Add(graph.Key, new List<int>());
                var context = contexts[graph.Key];
                if (context.EntryPoints != null)
                {
                    var r = graph.Value.KNearest(partial_k, context) as HashSet<int>;
                    if (r.Count < partial_k)
                    {
                        var diff = partial_k - r.Count;
                        partial_k += diff / (_graphs.Count - step);
                    }
                    result[graph.Key].AddRange(r);
                }
                step++;
            }
            return result;
        }
    }
}
