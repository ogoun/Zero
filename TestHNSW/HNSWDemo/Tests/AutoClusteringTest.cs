using System;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;

namespace HNSWDemo.Tests
{
    public class AutoClusteringTest
        : ITest
    {
        private static int Count = 3000;
        private static int Dimensionality = 128;

        public void Run()
        {
            var vectors = VectorUtils.RandomVectors(Dimensionality, Count);
            var world = SmallWorld.CreateWorld<float[]>(NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean));
            world.AddItems(vectors);
            var clusters = AutomaticGraphClusterer.DetectClusters(world);
            Console.WriteLine($"Found {clusters.Count} clusters");
            for (int i = 0; i < clusters.Count; i++)
            {
                Console.WriteLine($"Cluster {i + 1} countains {clusters[i].Count} items");
            }
        }
    }
}
