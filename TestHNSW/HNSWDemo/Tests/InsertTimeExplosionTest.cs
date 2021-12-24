using System;
using System.Diagnostics;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class InsertTimeExplosionTest
        : ITest
    {
        private static int Count = 10000;
        private static int IterationCount = 100;
        private static int Dimensionality = 128;

        public void Run()
        {
            var sw = new Stopwatch();
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 12, 100, 100, CosineDistance.NonOptimized));
            for (int i = 0; i < IterationCount; i++)
            {
                var samples = VectorUtils.RandomVectors(Dimensionality, Count);
                sw.Restart();
                var ids = world.AddItems(samples.ToArray());
                sw.Stop();
                Console.WriteLine($"ITERATION: [{i.ToString("D4")}] COUNT: [{ids.Length}] ELAPSED [{sw.ElapsedMilliseconds} ms]");
            }
        }
    }
}
