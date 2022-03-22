using HNSWDemo.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;
using ZeroLevel.Services.Mathemathics;

namespace HNSWDemo.Tests
{
    public class QuantizeAccuracityTest
        : ITest
    {
        private static int Count = 5000;
        private static int Dimensionality = 128;
        private static int K = 200;
        private static int TestCount =500;

        public void Run()
        {
            var totalHits = new List<int>();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();
            var q = new Quantizator(-1f, 1f);

            var s = VectorUtils.RandomVectors(Dimensionality, Count);
            var samples = s.Select(v => q.QuantizeToLong(v)).ToList();

            var sw = new Stopwatch();

            var test = new VectorsDirectCompare(s, Metrics.CosineDistance);
            var world = new SmallWorld<long[]>(NSWOptions<long[]>.Create(6, 8, 100, 100, Metrics.CosineDistance));

            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();

            Console.WriteLine($"Insert {ids.Length} items: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Start test");

            var tv = VectorUtils.RandomVectors(Dimensionality, TestCount);
            var test_vectors = tv.Select(v => q.QuantizeToLong(v)).ToList();
            for (int i = 0; i < tv.Count; i++)
            {
                sw.Restart();
                var gt = test.KNearest(tv[i], K).ToDictionary(p => p.Item1, p => p.Item2);
                sw.Stop();
                timewatchesNP.Add(sw.ElapsedMilliseconds);

                sw.Restart();
                var result = world.Search(test_vectors[i], K);
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
