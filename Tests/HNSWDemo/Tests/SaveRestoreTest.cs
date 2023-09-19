using System;
using System.Diagnostics;
using System.IO;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class SaveRestoreTest
        : ITest
    {
        private static int Count = 1000;
        private static int Dimensionality = 128;

        public void Run()
        {
            var samples = VectorUtils.RandomVectors(Dimensionality, Count);
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
    }
}
