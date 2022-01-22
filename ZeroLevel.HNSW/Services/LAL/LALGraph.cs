using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.HNSW
{
    public class LALGraph
        : IBinarySerializable
    {
        private readonly LALLinks _links = new LALLinks();

        public LALGraph() { }
        public static LALGraph FromLALGraph(Stream stream)
        {
            var l = new LALGraph();
            l.Deserialize(stream);
            return l;
        }

        public static LALGraph FromHNSWGraph<TItem>(Stream stream)
        {
            var l = new LALGraph();
            l.DeserializeFromHNSW<TItem>(stream);
            return l;
        }

        public IEnumerable<int> KNearest(int k, SearchContext context)
        {
            var v = new VisitedBitSet(_links.Count, 1);
            var C = new Queue<int>();
            var W = new HashSet<int>();
            var entryPoints = context.EntryPoints;
            var nextEntry = new HashSet<int>();
            do
            {
                foreach (var ep in entryPoints)
                {
                    var neighboursIds = _links.FindNeighbors(ep);
                    for (int i = 0; i < neighboursIds.Length; ++i)
                    {
                        if (v.Contains(neighboursIds[i]) == false)
                        {
                            C.Enqueue(neighboursIds[i]);
                            nextEntry.Add(neighboursIds[i]);
                        }
                    }
                    v.Add(ep);
                }
                // run bfs
                while (C.Count > 0)
                {
                    // get next candidate to check and expand
                    var toExpand = C.Dequeue();
                    if (context.IsActiveNode(toExpand))
                    {
                        if (W.Count < k)
                        {
                            W.Add(toExpand);
                            if (W.Count > k)
                            {
                                W.Remove(W.First());
                            }
                        }
                    }
                }
                entryPoints = nextEntry.Select(id => id).ToList();
                nextEntry.Clear();
            }
            while (W.Count < k && entryPoints.Any());
            C.Clear();
            v.Clear();
            return W;
        }

        public void Deserialize(Stream stream)
        {
            using (var reader = new MemoryStreamReader(stream))
            {
                _links.Deserialize(reader);
            }
        }

        public void DeserializeFromHNSW<TItem>(Stream stream)
        {
            using (var reader = new MemoryStreamReader(stream))
            {
                reader.ReadInt32(); //  EntryPoint
                reader.ReadInt32(); //  MaxLayer

                int count = reader.ReadInt32(); // Vectors count
                for (int i = 0; i < count; i++)
                {
                    reader.ReadCompatible<TItem>(); //  Vector
                }

                reader.ReadInt32(); //  countLayers
                _links.Deserialize(reader); // deserialize only base layer and skip another
            }
        }

        public void Serialize(Stream stream)
        {
            using (var writer = new MemoryStreamWriter(stream))
            {
                _links.Serialize(writer);
            }
        }

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
