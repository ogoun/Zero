﻿using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;

namespace HNSWDemo.Tests
{
    public class QuantizatorTest
        : ITest
    {
        private static int Count = 500000;
        private static int Dimensionality = 221;

        public void Run()
        {
            var samples = VectorUtils.RandomVectors(Dimensionality, Count);
            var min = samples.SelectMany(s => s).Min();
            var max = samples.SelectMany(s => s).Max();
            var q = new Quantizator(min, max);
            var q_samples = samples.Select(s => q.QuantizeToInt(s)).ToArray();

            // comparing
            var list = new List<float>();
            for (int i = 0; i < samples.Count - 1; i++)
            {
                var v1 = samples[i];
                var v2 = samples[i + 1];
                var dist = Metrics.Cosine(v1, v2);

                var qv1 = q_samples[i];
                var qv2 = q_samples[i + 1];
                var qdist = Metrics.Cosine(qv1, qv2);

                list.Add(Math.Abs(dist - qdist));
            }

            Console.WriteLine($"Min diff: {list.Min()}");
            Console.WriteLine($"Avg diff: {list.Average()}");
            Console.WriteLine($"Max diff: {list.Max()}");
        }
    }
}
