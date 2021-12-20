using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;

namespace HNSWDemo
{
    class Program
    {
        public class VectorsDirectCompare
        {
            private const int HALF_LONG_BITS = 32;
            private readonly IList<float[]> _vectors;
            private readonly Func<float[], float[], float> _distance;

            public VectorsDirectCompare(List<float[]> vectors, Func<float[], float[], float> distance)
            {
                _vectors = vectors;
                _distance = distance;
            }

            public IEnumerable<(int, float)> KNearest(float[] v, int k)
            {
                var weights = new Dictionary<int, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    var d = _distance(v, _vectors[i]);
                    weights[i] = d;
                }
                return weights.OrderBy(p => p.Value).Take(k).Select(p => (p.Key, p.Value));
            }

            public List<HashSet<int>> DetectClusters()
            {
                var links = new SortedList<long, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    for (int j = i + 1; j < _vectors.Count; j++)
                    {
                        long k = (((long)(i)) << HALF_LONG_BITS) + j;
                        links.Add(k, _distance(_vectors[i], _vectors[j]));
                    }
                }

                // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
                var histogram = new Histogram(HistogramMode.SQRT, links.Values);
                int threshold = histogram.OTSU();
                var min = histogram.Bounds[threshold - 1];
                var max = histogram.Bounds[threshold];
                var R = (max + min) / 2;

                // 2. Get links with distances less than R
                var resultLinks = new SortedList<long, float>();
                foreach (var pair in links)
                {
                    if (pair.Value < R)
                    {
                        resultLinks.Add(pair.Key, pair.Value);
                    }
                }

                // 3. Extract clusters
                List<HashSet<int>> clusters = new List<HashSet<int>>();
                foreach (var pair in resultLinks)
                {
                    var k = pair.Key;
                    var id1 = (int)(k >> HALF_LONG_BITS);
                    var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));

                    bool found = false;
                    foreach (var c in clusters)
                    {
                        if (c.Contains(id1))
                        {
                            c.Add(id2);
                            found = true;
                            break;
                        }
                        else if (c.Contains(id2))
                        {
                            c.Add(id1);
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        var c = new HashSet<int>();
                        c.Add(id1);
                        c.Add(id2);
                        clusters.Add(c);
                    }
                }
                return clusters;
            }
        }

        public class QVectorsDirectCompare
        {
            private const int HALF_LONG_BITS = 32;
            private readonly IList<byte[]> _vectors;
            private readonly Func<byte[], byte[], float> _distance;

            public QVectorsDirectCompare(List<byte[]> vectors, Func<byte[], byte[], float> distance)
            {
                _vectors = vectors;
                _distance = distance;
            }

            public IEnumerable<(int, float)> KNearest(byte[] v, int k)
            {
                var weights = new Dictionary<int, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    var d = _distance(v, _vectors[i]);
                    weights[i] = d;
                }
                return weights.OrderBy(p => p.Value).Take(k).Select(p => (p.Key, p.Value));
            }

            public List<HashSet<int>> DetectClusters()
            {
                var links = new SortedList<long, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    for (int j = i + 1; j < _vectors.Count; j++)
                    {
                        long k = (((long)(i)) << HALF_LONG_BITS) + j;
                        links.Add(k, _distance(_vectors[i], _vectors[j]));
                    }
                }

                // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
                var histogram = new Histogram(HistogramMode.SQRT, links.Values);
                int threshold = histogram.OTSU();
                var min = histogram.Bounds[threshold - 1];
                var max = histogram.Bounds[threshold];
                var R = (max + min) / 2;

                // 2. Get links with distances less than R
                var resultLinks = new SortedList<long, float>();
                foreach (var pair in links)
                {
                    if (pair.Value < R)
                    {
                        resultLinks.Add(pair.Key, pair.Value);
                    }
                }

                // 3. Extract clusters
                List<HashSet<int>> clusters = new List<HashSet<int>>();
                foreach (var pair in resultLinks)
                {
                    var k = pair.Key;
                    var id1 = (int)(k >> HALF_LONG_BITS);
                    var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));

                    bool found = false;
                    foreach (var c in clusters)
                    {
                        if (c.Contains(id1))
                        {
                            c.Add(id2);
                            found = true;
                            break;
                        }
                        else if (c.Contains(id2))
                        {
                            c.Add(id1);
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        var c = new HashSet<int>();
                        c.Add(id1);
                        c.Add(id2);
                        clusters.Add(c);
                    }
                }
                return clusters;
            }
        }

        public class QLVectorsDirectCompare
        {
            private const int HALF_LONG_BITS = 32;
            private readonly IList<long[]> _vectors;
            private readonly Func<long[], long[], float> _distance;

            public QLVectorsDirectCompare(List<long[]> vectors, Func<long[], long[], float> distance)
            {
                _vectors = vectors;
                _distance = distance;
            }

            public IEnumerable<(int, float)> KNearest(long[] v, int k)
            {
                var weights = new Dictionary<int, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    var d = _distance(v, _vectors[i]);
                    weights[i] = d;
                }
                return weights.OrderBy(p => p.Value).Take(k).Select(p => (p.Key, p.Value));
            }

            public List<HashSet<int>> DetectClusters()
            {
                var links = new SortedList<long, float>();
                for (int i = 0; i < _vectors.Count; i++)
                {
                    for (int j = i + 1; j < _vectors.Count; j++)
                    {
                        long k = (((long)(i)) << HALF_LONG_BITS) + j;
                        links.Add(k, _distance(_vectors[i], _vectors[j]));
                    }
                }

                // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
                var histogram = new Histogram(HistogramMode.SQRT, links.Values);
                int threshold = histogram.OTSU();
                var min = histogram.Bounds[threshold - 1];
                var max = histogram.Bounds[threshold];
                var R = (max + min) / 2;

                // 2. Get links with distances less than R
                var resultLinks = new SortedList<long, float>();
                foreach (var pair in links)
                {
                    if (pair.Value < R)
                    {
                        resultLinks.Add(pair.Key, pair.Value);
                    }
                }

                // 3. Extract clusters
                List<HashSet<int>> clusters = new List<HashSet<int>>();
                foreach (var pair in resultLinks)
                {
                    var k = pair.Key;
                    var id1 = (int)(k >> HALF_LONG_BITS);
                    var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));

                    bool found = false;
                    foreach (var c in clusters)
                    {
                        if (c.Contains(id1))
                        {
                            c.Add(id2);
                            found = true;
                            break;
                        }
                        else if (c.Contains(id2))
                        {
                            c.Add(id1);
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        var c = new HashSet<int>();
                        c.Add(id1);
                        c.Add(id2);
                        clusters.Add(c);
                    }
                }
                return clusters;
            }
        }

        public enum Gender
        {
            Unknown, Male, Feemale
        }

        public class Person
        {
            public Gender Gender { get; set; }
            public int Age { get; set; }
            public long Number { get; set; }

            private static (float[], Person) Generate(int vector_size)
            {
                var rnd = new Random((int)Environment.TickCount);
                var vector = new float[vector_size];
                DefaultRandomGenerator.Instance.NextFloats(vector);
                VectorUtils.NormalizeSIMD(vector);
                var p = new Person();
                p.Age = rnd.Next(15, 80);
                var gr = rnd.Next(0, 3);
                p.Gender = (gr == 0) ? Gender.Male : (gr == 1) ? Gender.Feemale : Gender.Unknown;
                p.Number = CreateNumber(rnd);
                return (vector, p);
            }

            public static List<(float[], Person)> GenerateRandom(int vectorSize, int vectorsCount)
            {
                var vectors = new List<(float[], Person)>();
                for (int i = 0; i < vectorsCount; i++)
                {
                    vectors.Add(Generate(vectorSize));
                }
                return vectors;
            }

            static HashSet<long> _exists = new HashSet<long>();
            private static long CreateNumber(Random rnd)
            {
                long start_number;
                do
                {
                    start_number = 79600000000L;
                    start_number = start_number + rnd.Next(4, 8) * 10000000;
                    start_number += rnd.Next(0, 1000000);
                }
                while (_exists.Add(start_number) == false);
                return start_number;
            }
        }

        private static List<float[]> RandomVectors(int vectorSize, int vectorsCount)
        {
            var vectors = new List<float[]>();
            for (int i = 0; i < vectorsCount; i++)
            {
                var vector = new float[vectorSize];
                DefaultRandomGenerator.Instance.NextFloats(vector);
                VectorUtils.NormalizeSIMD(vector);
                vectors.Add(vector);
            }
            return vectors;
        }


        static void Main(string[] args)
        {
            QuantizatorTest();
            Console.WriteLine("Completed");
            Console.ReadKey();
        }

        static void QAccuracityTest()
        {
            int K = 200;
            var count = 5000;
            var testCount = 500;
            var dimensionality = 128;
            var totalHits = new List<int>();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();
            var q = new Quantizator(-1f, 1f);

            var samples = RandomVectors(dimensionality, count).Select(v => q.QuantizeToLong(v)).ToList();

            var sw = new Stopwatch();

            var test = new QLVectorsDirectCompare(samples, CosineDistance.NonOptimized);
            var world = new SmallWorld<long[]>(NSWOptions<long[]>.Create(8, 12, 100, 100, CosineDistance.NonOptimized));

            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();

            Console.WriteLine($"Insert {ids.Length} items: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Start test");


            var test_vectors = RandomVectors(dimensionality, testCount).Select(v => q.QuantizeToLong(v)).ToList();
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

        static void QInsertTimeExplosionTest()
        {
            var count = 10000;
            var iterationCount = 100;
            var dimensionality = 128;
            var sw = new Stopwatch();
            var world = new SmallWorld<long[]>(NSWOptions<long[]>.Create(6, 12, 100, 100, CosineDistance.NonOptimized));
            var q = new Quantizator(-1f, 1f);
            for (int i = 0; i < iterationCount; i++)
            {
                var samples = RandomVectors(dimensionality, count);
                sw.Restart();
                var ids = world.AddItems(samples.Select(v => q.QuantizeToLong(v)).ToArray());
                sw.Stop();
                Console.WriteLine($"ITERATION: [{i.ToString("D4")}] COUNT: [{ids.Length}] ELAPSED [{sw.ElapsedMilliseconds} ms]");
            }
        }

        static void AccuracityTest()
        {
            int K = 200;
            var count = 3000;
            var testCount = 500;
            var dimensionality = 128;
            var totalHits = new List<int>();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();

            var samples = RandomVectors(dimensionality, count);

            var sw = new Stopwatch();

            var test = new VectorsDirectCompare(samples, CosineDistance.NonOptimized);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(8, 12, 100, 100, CosineDistance.NonOptimized));

            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();

            /*
            byte[] dump;
            using (var ms = new MemoryStream())
            {
                world.Serialize(ms);
                dump = ms.ToArray();
            }
            Console.WriteLine($"Full dump size: {dump.Length} bytes");

            
            ReadOnlySmallWorld<float[]> world;
            using (var ms = new MemoryStream(dump))
            {
                world = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(100, CosineDistance.NonOptimized, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple), ms);
            }
            */

            Console.WriteLine($"Insert {ids.Length} items: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Start test");


            var test_vectors = RandomVectors(dimensionality, testCount);
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

        static void QuantizatorTest()
        {
            var samples = RandomVectors(128, 500000);
            var min = samples.SelectMany(s => s).Min();
            var max = samples.SelectMany(s => s).Max();
            var q = new Quantizator(min, max);
            var q_samples = samples.Select(s => q.QuantizeToLong(s)).ToArray();

            // comparing
            var list = new List<float>();
            for (int i = 0; i < samples.Count - 1; i++)
            {
                var v1 = samples[i];
                var v2 = samples[i + 1];
                var dist = CosineDistance.NonOptimized(v1, v2);

                var qv1 = q_samples[i];
                var qv2 = q_samples[i + 1];
                var qdist = CosineDistance.NonOptimized(qv1, qv2);

                list.Add(Math.Abs(dist - qdist));
            }

            Console.WriteLine($"Min diff: {list.Min()}");
            Console.WriteLine($"Avg diff: {list.Average()}");
            Console.WriteLine($"Max diff: {list.Max()}");
        }

        static void SaveRestoreTest()
        {
            var count = 1000;
            var dimensionality = 128;
            var samples = RandomVectors(dimensionality, count);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits));
            var sw = new Stopwatch();
            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();
            Console.WriteLine($"Insert {ids.Length} items on {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Start test");

            byte[] dump;
            using (var ms = new MemoryStream())
            {
                world.Serialize(ms);
                dump = ms.ToArray();
            }
            Console.WriteLine($"Full dump size: {dump.Length} bytes");

            byte[] testDump;
            var restoredWorld = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits));
            using (var ms = new MemoryStream(dump))
            {
                restoredWorld.Deserialize(ms);
            }

            using (var ms = new MemoryStream())
            {
                restoredWorld.Serialize(ms);
                testDump = ms.ToArray();
            }
            if (testDump.Length != dump.Length)
            {
                Console.WriteLine($"Incorrect restored size. Got {testDump.Length}. Expected: {dump.Length}");
                return;
            }
        }

        static void InsertTimeExplosionTest()
        {
            var count = 10000;
            var iterationCount = 100;
            var dimensionality = 128;
            var sw = new Stopwatch();
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 12, 100, 100, CosineDistance.NonOptimized));
            for (int i = 0; i < iterationCount; i++)
            {
                var samples = RandomVectors(dimensionality, count);
                sw.Restart();
                var ids = world.AddItems(samples.ToArray());
                sw.Stop();
                Console.WriteLine($"ITERATION: [{i.ToString("D4")}] COUNT: [{ids.Length}] ELAPSED [{sw.ElapsedMilliseconds} ms]");
            }
        }

        /*
        static void TestOnMnist()
        {
            int imageCount, rowCount, colCount;
            var buf = new byte[4];
            var image = new byte[28 * 28];
            var vectors = new List<float[]>();
            using (var fs = new FileStream("t10k-images.idx3-ubyte", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // first 4 bytes is a magic number
                fs.Read(buf, 0, 4);
                // second 4 bytes is the number of images
                fs.Read(buf, 0, 4);
                imageCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);
                // third 4 bytes is the row count
                fs.Read(buf, 0, 4);
                rowCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);
                // fourth 4 bytes is the column count
                fs.Read(buf, 0, 4);
                colCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);

                for (int i = 0; i < imageCount; i++)
                {
                    fs.Read(image, 0, image.Length);
                    vectors.Add(image.Select(b => (float)b).ToArray());
                }
            }

            //var direct = new VectorsDirectCompare(vectors, Metrics.L2Euclidean);
            
            var options = NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple);
            SmallWorld<float[]> world;
            if (File.Exists("graph.bin"))
            {
                using (var fs = new FileStream("graph.bin", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    world = SmallWorld.CreateWorldFrom<float[]>(options, fs);
                }
            }
            else
            {
                world = SmallWorld.CreateWorld<float[]>(options);
                world.AddItems(vectors);
                using (var fs = new FileStream("graph.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    world.Serialize(fs);
                }
            }
            
            var clusters = AutomaticGraphClusterer.DetectClusters(world);
            Console.WriteLine($"Found {clusters.Count} clusters");
            for (int i = 0; i < clusters.Count; i++)
            {
                Console.WriteLine($"Cluster {i + 1} countains {clusters[i].Count} items");
            }

        }

         static void AutoClusteringTest()
         {
             var vectors = RandomVectors(128, 3000);
             var world = SmallWorld.CreateWorld<float[]>(NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
             world.AddItems(vectors);
             var clusters = AutomaticGraphClusterer.DetectClusters(world);
             Console.WriteLine($"Found {clusters.Count} clusters");
             for (int i = 0; i < clusters.Count; i++)
             {
                 Console.WriteLine($"Cluster {i + 1} countains {clusters[i].Count} items");
             }
         }

         static void HistogramTest()
         {
             var vectors = RandomVectors(128, 3000);
             var world = SmallWorld.CreateWorld<float[]>(NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
             world.AddItems(vectors);
             var histogram = world.GetHistogram();

             int threshold = histogram.OTSU();
             var min = histogram.Bounds[threshold - 1];
             var max = histogram.Bounds[threshold];
             var R = (max + min) / 2;

             DrawHistogram(histogram, @"D:\hist.jpg");
         }

        static void DrawHistogram(Histogram histogram, string filename)
        {
        var wb = 1200 / histogram.Values.Length;
        var k = 600.0f / (float)histogram.Values.Max();

        var maxes = histogram.GetMaximums().ToDictionary(m => m.Index, m => m);
        int threshold = histogram.OTSU();

            using (var bmp = new Bitmap(1200, 600))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    for (int i = 0; i<histogram.Values.Length; i++)
                    {
                        var height = (int)(histogram.Values[i] * k);
                        if (maxes.ContainsKey(i))
                        {
                            g.DrawRectangle(Pens.Red, i* wb, bmp.Height - height, wb, height);
                            g.DrawRectangle(Pens.Red, i* wb + 1, bmp.Height - height, wb - 1, height);
                        }
                        else
{
    g.DrawRectangle(Pens.Blue, i * wb, bmp.Height - height, wb, height);
}
if (i == threshold)
{
    g.DrawLine(Pens.Green, i * wb + wb / 2, 0, i * wb + wb / 2, bmp.Height);
}
                    }
                }
                bmp.Save(filename);
            }
        } 

        static void TransformToCompactWorldTest()
        {
            var count = 10000;
            var dimensionality = 128;
            var samples = RandomVectors(dimensionality, count);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits));
            var ids = world.AddItems(samples.ToArray());

            Console.WriteLine("Start test");

            byte[] dump;
            using (var ms = new MemoryStream())
            {
                world.Serialize(ms);
                dump = ms.ToArray();
            }
            Console.WriteLine($"Full dump size: {dump.Length} bytes");

            ReadOnlySmallWorld<float[]> compactWorld;
            using (var ms = new MemoryStream(dump))
            {
                compactWorld = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(200, CosineDistance.ForUnits), ms);
            }

            // Compare worlds outputs
            int K = 200;
            var hits = 0;
            var miss = 0;
            var testCount = 1000;
            var sw = new Stopwatch();
            var timewatchesHNSW = new List<float>();
            var timewatchesHNSWCompact = new List<float>();
            var test_vectors = RandomVectors(dimensionality, testCount);

            foreach (var v in test_vectors)
            {
                sw.Restart();
                var gt = world.Search(v, K).Select(e => e.Item1).ToHashSet();
                sw.Stop();
                timewatchesHNSW.Add(sw.ElapsedMilliseconds);

                sw.Restart();
                var result = compactWorld.Search(v, K).Select(e => e.Item1).ToHashSet();
                sw.Stop();
                timewatchesHNSWCompact.Add(sw.ElapsedMilliseconds);

                foreach (var r in result)
                {
                    if (gt.Contains(r))
                    {
                        hits++;
                    }
                    else
                    {
                        miss++;
                    }
                }
            }

            byte[] smallWorldDump;
            using (var ms = new MemoryStream())
            {
                compactWorld.Serialize(ms);
                smallWorldDump = ms.ToArray();
            }
            var p = smallWorldDump.Length * 100.0f / dump.Length;
            Console.WriteLine($"Compact dump size: {smallWorldDump.Length} bytes. Decrease: {100 - p}%");

            Console.WriteLine($"HITS: {hits}");
            Console.WriteLine($"MISSES: {miss}");

            Console.WriteLine($"MIN HNSW TIME: {timewatchesHNSW.Min()} ms");
            Console.WriteLine($"AVG HNSW TIME: {timewatchesHNSW.Average()} ms");
            Console.WriteLine($"MAX HNSW TIME: {timewatchesHNSW.Max()} ms");

            Console.WriteLine($"MIN HNSWCompact TIME: {timewatchesHNSWCompact.Min()} ms");
            Console.WriteLine($"AVG HNSWCompact TIME: {timewatchesHNSWCompact.Average()} ms");
            Console.WriteLine($"MAX HNSWCompact TIME: {timewatchesHNSWCompact.Max()} ms");
        }

        static void TransformToCompactWorldTestWithAccuracity()
        {
            var count = 10000;
            var dimensionality = 128;
            var samples = RandomVectors(dimensionality, count);

            var test = new VectorsDirectCompare(samples, CosineDistance.ForUnits);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits));
            var ids = world.AddItems(samples.ToArray());

            Console.WriteLine("Start test");

            byte[] dump;
            using (var ms = new MemoryStream())
            {
                world.Serialize(ms);
                dump = ms.ToArray();
            }

            ReadOnlySmallWorld<float[]> compactWorld;
            using (var ms = new MemoryStream(dump))
            {
                compactWorld = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(200, CosineDistance.ForUnits), ms);
            }

            // Compare worlds outputs
            int K = 200;
            var hits = 0;
            var miss = 0;

            var testCount = 2000;
            var sw = new Stopwatch();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();
            var timewatchesHNSWCompact = new List<float>();
            var test_vectors = RandomVectors(dimensionality, testCount);

            var totalHitsHNSW = new List<int>();
            var totalHitsHNSWCompact = new List<int>();

            foreach (var v in test_vectors)
            {
                var npHitsHNSW = 0;
                var npHitsHNSWCompact = 0;

                sw.Restart();
                var gtNP = test.KNearest(v, K).Select(p => p.Item1).ToHashSet();
                sw.Stop();
                timewatchesNP.Add(sw.ElapsedMilliseconds);

                sw.Restart();
                var gt = world.Search(v, K).Select(e => e.Item1).ToHashSet();
                sw.Stop();
                timewatchesHNSW.Add(sw.ElapsedMilliseconds);

                sw.Restart();
                var result = compactWorld.Search(v, K).Select(e => e.Item1).ToHashSet();
                sw.Stop();
                timewatchesHNSWCompact.Add(sw.ElapsedMilliseconds);

                foreach (var r in result)
                {
                    if (gt.Contains(r))
                    {
                        hits++;
                    }
                    else
                    {
                        miss++;
                    }
                    if (gtNP.Contains(r))
                    {
                        npHitsHNSWCompact++;
                    }
                }

                foreach (var r in gt)
                {
                    if (gtNP.Contains(r))
                    {
                        npHitsHNSW++;
                    }
                }

                totalHitsHNSW.Add(npHitsHNSW);
                totalHitsHNSWCompact.Add(npHitsHNSWCompact);
            }

            byte[] smallWorldDump;
            using (var ms = new MemoryStream())
            {
                compactWorld.Serialize(ms);
                smallWorldDump = ms.ToArray();
            }
            var p = smallWorldDump.Length * 100.0f / dump.Length;
            Console.WriteLine($"Full dump size: {dump.Length} bytes");
            Console.WriteLine($"Compact dump size: {smallWorldDump.Length} bytes. Decrease: {100 - p}%");

            Console.WriteLine($"HITS: {hits}");
            Console.WriteLine($"MISSES: {miss}");

            Console.WriteLine($"MIN NP TIME: {timewatchesNP.Min()} ms");
            Console.WriteLine($"AVG NP TIME: {timewatchesNP.Average()} ms");
            Console.WriteLine($"MAX NP TIME: {timewatchesNP.Max()} ms");

            Console.WriteLine($"MIN HNSW TIME: {timewatchesHNSW.Min()} ms");
            Console.WriteLine($"AVG HNSW TIME: {timewatchesHNSW.Average()} ms");
            Console.WriteLine($"MAX HNSW TIME: {timewatchesHNSW.Max()} ms");

            Console.WriteLine($"MIN HNSWCompact TIME: {timewatchesHNSWCompact.Min()} ms");
            Console.WriteLine($"AVG HNSWCompact TIME: {timewatchesHNSWCompact.Average()} ms");
            Console.WriteLine($"MAX HNSWCompact TIME: {timewatchesHNSWCompact.Max()} ms");

            Console.WriteLine($"MIN HNSW Accuracity: {totalHitsHNSW.Min() * 100 / K}%");
            Console.WriteLine($"AVG HNSW Accuracity: {totalHitsHNSW.Average() * 100 / K}%");
            Console.WriteLine($"MAX HNSW Accuracity: {totalHitsHNSW.Max() * 100 / K}%");

            Console.WriteLine($"MIN HNSWCompact Accuracity: {totalHitsHNSWCompact.Min() * 100 / K}%");
            Console.WriteLine($"AVG HNSWCompact Accuracity: {totalHitsHNSWCompact.Average() * 100 / K}%");
            Console.WriteLine($"MAX HNSWCompact Accuracity: {totalHitsHNSWCompact.Max() * 100 / K}%");
        }
        
         static void FilterTest()
        {
            var count = 1000;
            var testCount = 100;
            var dimensionality = 128;
            var samples = Person.GenerateRandom(dimensionality, count);

            var testDict = samples.ToDictionary(s => s.Item2.Number, s => s.Item2);

            var map = new HNSWMap<long>();
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));

            var ids = world.AddItems(samples.Select(i => i.Item1).ToArray());
            for (int bi = 0; bi < samples.Count; bi++)
            {
                map.Append(samples[bi].Item2.Number, ids[bi]);
            }

            Console.WriteLine("Start test");
            int K = 200;
            var vectors = RandomVectors(dimensionality, testCount);

            var context = new SearchContext()
                .SetActiveNodes(map
                    .ConvertFeaturesToIds(samples
                        .Where(p => p.Item2.Age > 20 && p.Item2.Age < 50 && p.Item2.Gender == Gender.Feemale)
                        .Select(p => p.Item2.Number)));

            var hits = 0;
            var miss = 0;
            foreach (var v in vectors)
            {
                var numbers = map.ConvertIdsToFeatures(world.Search(v, K, context).Select(r => r.Item1));
                foreach (var r in numbers)
                {
                    var record = testDict[r];
                    if (record.Gender == Gender.Feemale && record.Age > 20 && record.Age < 50)
                    {
                        hits++;
                    }
                    else
                    {
                        miss++;
                    }
                }
            }
            Console.WriteLine($"SUCCESS: {hits}");
            Console.WriteLine($"ERROR: {miss}");
        }
        */
    }
}
