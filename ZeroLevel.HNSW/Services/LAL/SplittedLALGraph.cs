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

        public IEnumerable<int> KNearest(int k, IDictionary<int, SearchContext> contexts)
        {
            var partial_k = 1 + (k / _graphs.Count);
            var result = new List<int>();
            foreach (var graph in _graphs)
            {
                var context = contexts[graph.Key];
                if (context.EntryPoints != null)
                {
                    result.AddRange(graph.Value.KNearest(partial_k, context));
                }
            }
            return result;
        }
    }
}
