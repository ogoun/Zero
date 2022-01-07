using HNSWDemo.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class AccuracityTest
        : ITest
    {
        private static int K = 200;
        private static int count = 10000;
        private static int testCount = 500;
        private static int dimensionality = 128;

        public void Run()
        {
            var totalHits = new List<int>();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();

            var samples = VectorUtils.RandomVectors(dimensionality, count);

            var sw = new Stopwatch();

            var test = new VectorsDirectCompare(samples, Metrics.Cosine);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(8, 12, 100, 100, Metrics.Cosine));

            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();

            Console.WriteLine($"Insert {ids.Length} items: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Start test");

            var test_vectors = VectorUtils.RandomVectors(dimensionality, testCount);
            foreach (var v in test_vectors)
            {
                sw.Restart();
                var gt = test.KNearest(v, K).ToDictionary(p => p.Item1, p => p.Item2);
                sw.Stop();
                timewatchesNP.Add(sw.ElapsedMilliseconds);

                sw.Restart();
                var result = world.Search(v, K);
                sw.Stop();

                timewatchesHNSW.Add(sw.ElapsedMilliseconds);
                var hits = 0;
                foreach (var r in result)
                {
                    if (gt.ContainsKey(r.Item1))
                    {
                        hits++;
                    }
                }
                totalHits.Add(hits);
            }

            Console.WriteLine($"MIN Accuracity: {totalHits.Min() * 100 / K}%");
            Console.WriteLine($"AVG Accuracity: {totalHits.Average() * 100 / K}%");
            Console.WriteLine($"MAX Accuracity: {totalHits.Max() * 100 / K}%");

            Console.WriteLine($"MIN HNSW TIME: {timewatchesHNSW.Min()} ms");
            Console.WriteLine($"AVG HNSW TIME: {timewatchesHNSW.Average()} ms");
            Console.WriteLine($"MAX HNSW TIME: {timewatchesHNSW.Max()} ms");

            Console.WriteLine($"MIN NP TIME: {timewatchesNP.Min()} ms");
            Console.WriteLine($"AVG NP TIME: {timewatchesNP.Average()} ms");
            Console.WriteLine($"MAX NP TIME: {timewatchesNP.Max()} ms");
        }
    }
}
