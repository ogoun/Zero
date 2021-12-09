using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var dimensionality = 128;
            var testCount = 1000;
            var count = 5000;
            var samples = Person.GenerateRandom(dimensionality, count);

            var sw = new Stopwatch();

            var test = new VectorsDirectCompare(samples.Select(s => s.Item1).ToList(), CosineDistance.ForUnits);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits, true, true, selectionHeuristic: NeighbourSelectionHeuristic.SelectSimple));

            var batch = samples.ToArray();
            
            var ids = world.AddItems(batch.Select(i => i.Item1).ToArray());
            
            Console.WriteLine($"Insert {ids.Length} items on {sw.ElapsedMilliseconds} ms");
            for (int bi = 0; bi < batch.Length; bi++)
            {
                _database.Add(ids[bi], batch[bi].Item2);
            }

            Console.WriteLine("Start test");
            int K = 200;
            var vectors = RandomVectors(dimensionality, testCount);
            var totalHits = new List<int>();
            var timewatchesHNSW = new List<float>();
            var timewatchesNP = new List<float>();
            foreach (var v in vectors)
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



            //HNSWFilter filter = new HNSWFilter(ids => ids.Where(id => { var p = _database[id]; return p.Age > 45 && p.Gender == Gender.Feemale; }));

            /*var fackupCount = 0;
                        foreach (var v in vectors)
                        {
                            var result = world.Search(v, 10, filter);                
                            foreach (var r in result)
                            {
                                if (_database[r.Item1].Age <= 45 || _database[r.Item1].Gender != Gender.Feemale)
                                {
                                    Interlocked.Increment(ref fackupCount);
                                }
                            }
                        }*/

            //Console.WriteLine($"Completed. Fackup count: {fackupCount}");
            Console.ReadKey();
        }
    }
}
