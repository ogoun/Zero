using System;
using System.Diagnostics;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;

namespace HNSWDemo.Tests
{
    public class QuantizeInsertTimeExplosionTest
        : ITest
    {
        private static int Count = 10000;
        private static int IterationCount = 100;
        private static int Dimensionality = 128;

        public void Run()
        {            
            var sw = new Stopwatch();
            var world = new SmallWorld<long[]>(NSWOptions<long[]>.Create(6, 12, 100, 100, CosineDistance.NonOptimized));
            var q = new Quantizator(-1f, 1f);
            for (int i = 0; i < IterationCount; i++)
            {
                var samples = VectorUtils.RandomVectors(Dimensionality, Count);
                sw.Restart();
                var ids = world.AddItems(samples.Select(v => q.QuantizeToLong(v)).ToArray());
                sw.Stop();
                Console.WriteLine($"ITERATION: [{i.ToString("D4")}] COUNT: [{ids.Length}] ELAPSED [{sw.ElapsedMilliseconds} ms]");
            }
        }
    }
}
