using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ZeroLevel.HNSW;

namespace HNSWDemo
{
    class Program
    {
        public class VectorsDirectCompare
        {
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

        private static Dictionary<int, Person> _database = new Dictionary<int, Person>();

        static void Main(string[] args)
        {
            FilterTest();
            Console.ReadKey();
        }

        static void TransformToCompactWorldTest()
        {
            var count = 10000;
            var dimensionality = 128;
            var samples = RandomVectors(dimensionality, count);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
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
                compactWorld = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple), ms);
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
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
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
                compactWorld = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple), ms);
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

        static void SaveRestoreTest()
        {
            var count = 1000;
            var dimensionality = 128;
            var samples = RandomVectors(dimensionality, count);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
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
            var restoredWorld = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));
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

            ReadOnlySmallWorld<float[]> compactWorld;
            using (var ms = new MemoryStream(dump))
            {
                compactWorld = SmallWorld.CreateReadOnlyWorldFrom<float[]>(NSWReadOnlyOption<float[]>.Create(200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple), ms);
            }

            byte[] smallWorldDump;
            using (var ms = new MemoryStream())
            {
                compactWorld.Serialize(ms);
                smallWorldDump = ms.ToArray();
            }
            var p = smallWorldDump.Length * 100.0f / dump.Length;
            Console.WriteLine($"Compact dump size: {smallWorldDump.Length} bytes. Decrease: {100 - p}%");
        }

        static void FilterTest()
        {
            var count = 1000;
            var testCount = 100;
            var dimensionality = 128;
            var samples = Person.GenerateRandom(dimensionality, count);

            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));

            var ids = world.AddItems(samples.Select(i => i.Item1).ToArray());
            for (int bi = 0; bi < samples.Count; bi++)
            {
                _database.Add(ids[bi], samples[bi].Item2);
            }

            Console.WriteLine("Start test");
            int K = 200;
            var vectors = RandomVectors(dimensionality, testCount);

            var context = new SearchContext().SetActiveNodes(_database.Where(pair => pair.Value.Age > 20 && pair.Value.Age < 50 && pair.Value.Gender == Gender.Feemale).Select(pair => pair.Key));

            var hits = 0;
            var miss = 0;
            foreach (var v in vectors)
            {
                var result = world.Search(v, K, context);
                foreach (var r in result)
                {
                    var record = _database[r.Item1];
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

        static void AccuracityTest()
        {
            int K = 200;
            var count = 5000;
            var testCount = 1000;
            var dimensionality = 128;
            var totalHits = new List<int>();
            var timewatchesNP = new List<float>();
            var timewatchesHNSW = new List<float>();
            var samples = RandomVectors(dimensionality, count);

            var sw = new Stopwatch();

            var test = new VectorsDirectCompare(samples, CosineDistance.ForUnits);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));

            sw.Start();
            var ids = world.AddItems(samples.ToArray());
            sw.Stop();

            Console.WriteLine($"Insert {ids.Length} items on {sw.ElapsedMilliseconds} ms");
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
    }
}
