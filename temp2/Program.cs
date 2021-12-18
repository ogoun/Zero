using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.Services.Serialization;

namespace temp2
{
    class Program
    {
        static void Main(string[] args)
        {
            SmallWorld<float[]> world;
            using (var ms = new FileStream(@"F:\graph_test.bin", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 12, 100, 10, Metrics.L2Euclidean, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple), ms);
            }

            var test_vectors = new List<float[]>();
            using (var ms = new FileStream(@"F:\test_vectors.bin", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var reader = new MemoryStreamReader(ms))
                {
                    var count = reader.ReadInt32();
                    for(int i=0;i<count; i++)
                    {
                        test_vectors.Add(reader.ReadFloatArray());
                    }
                }
            }
            Forward(world, test_vectors);
            Console.WriteLine("Completed");
        }

        static void Forward(SmallWorld<float[]> world, List<float[]> test_vectors)
        {
            int K = 10;
            foreach (var v in test_vectors)
            {
                var result = world.Search(v, K);
                Console.WriteLine(result.Count());
            }
        }
    }
}
