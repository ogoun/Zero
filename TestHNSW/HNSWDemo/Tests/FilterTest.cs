using HNSWDemo.Model;
using System;
using System.Linq;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class FilterTest
        : ITest
    {
        private const int count = 3000;
        private const int testCount = 100;
        private const int dimensionality = 128;

        public void Run()
        {
            var map = new HNSWMap<long>();
            var samples = Person.GenerateRandom(dimensionality, count);
            var testDict = samples.ToDictionary(s => s.Item2.Number, s => s.Item2);
            var world = new SmallWorld<float[]>(NSWOptions<float[]>.Create(6, 15, 200, 200, CosineDistance.ForUnits));
            var ids = world.AddItems(samples.Select(i => i.Item1).ToArray());
            for (int bi = 0; bi < samples.Count; bi++)
            {
                map.Append(samples[bi].Item2.Number, ids[bi]);
            }
            Console.WriteLine("Start test");
            int K = 200;
            var vectors = VectorUtils.RandomVectors(dimensionality, testCount);

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
                    if (context.NodeCheckMode == Mode.None || (record.Gender == Gender.Feemale && record.Age > 20 && record.Age < 50))
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
    }
}
