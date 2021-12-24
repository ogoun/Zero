using HNSWDemo.Tests;
using System;

namespace HNSWDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            new AutoClusteringTest().Run();
            Console.WriteLine("Completed");
            Console.ReadKey();
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
