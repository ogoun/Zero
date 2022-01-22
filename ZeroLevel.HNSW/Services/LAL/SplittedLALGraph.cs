using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    public class SplittedLALGraph
        : IBinarySerializable
    {
        private IDictionary<int, LALGraph> _graphs;

        public SplittedLALGraph()
        {
            _graphs = new Dictionary<int, LALGraph>();
        }

        public SplittedLALGraph(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                using (var bs = new BufferedStream(fs, 1024 * 1024 * 32))
                {
                    using (var reader = new MemoryStreamReader(bs))
                    {
                        Deserialize(reader);
                    }
                }
            }
        }

        public void Save(string filePath)
        {
            using (var fs = File.OpenWrite(filePath))
            {
                using (var bs = new BufferedStream(fs, 1024 * 1024 * 32))
                {
                    using (var writer = new MemoryStreamWriter(bs))
                    {
                        Serialize(writer);
                    }
                }
            }
        }

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

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteDictionary<int, LALGraph>(this._graphs);
        }
        public void Deserialize(IBinaryReader reader)
        {
            this._graphs = reader.ReadDictionary<int, LALGraph>();
        }
    }
}
