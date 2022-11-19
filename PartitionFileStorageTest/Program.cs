using System.Diagnostics;
using System.Text;
using ZeroLevel;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage;

namespace PartitionFileStorageTest
{
    internal class Program
    {
        private class Metadata
        {
            public DateTime Date { get; set; }
        }

        private static ulong Generate(Random r)
        {
            var num = new StringBuilder();
            num.Append("79");
            num.Append(r.Next(99).ToString("D2"));
            num.Append(r.Next(999).ToString("D7"));
            return ulong.Parse(num.ToString());
        }

        private static void FastTest(string root)
        {
            var r = new Random(Environment.TickCount);
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions { Enabled = true, FileIndexCount = 64 },
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 128).ToString()),
                MergeFunction = list =>
                {
                    ulong s = 0;
                    return Compressor.GetEncodedBytes(list.OrderBy(c => c), ref s);
                },
                Partitions = new List<StoreCatalogPartition<Metadata>>
                {
                    new StoreCatalogPartition<Metadata>("Date", m => m.Date.ToString("yyyyMMdd"))
                },
                KeyComparer = (left, right) => left == right ? 0 : (left < right) ? -1 : 1,
            };
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var storePart = store.CreateBuilder(new Metadata { Date = new DateTime(2022, 11, 08) });

            Console.WriteLine("Small test start");
            var c1 = (ulong)(86438 * 128);
            var c2 = (ulong)(83438 * 128);
            var c3 = (ulong)(831238 * 128);

            storePart.Store(c1, Generate(r));
            storePart.Store(c1, Generate(r));
            storePart.Store(c1, Generate(r));
            storePart.Store(c2, Generate(r));
            storePart.Store(c2, Generate(r));
            storePart.Store(c2, Generate(r));
            storePart.Store(c2, Generate(r));
            storePart.Store(c2, Generate(r));
            storePart.Store(c3, Generate(r));
            storePart.Store(c3, Generate(r));
            storePart.Store(c3, Generate(r));
            storePart.CompleteAdding();
            storePart.Compress();
            var readPart = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            Console.WriteLine("Data:");
            foreach (var e in readPart.Iterate())
            {
                Console.WriteLine($"{e.Key}: {e.Value.Length}");
            }
            readPart.RemoveKey(c1);
            Console.WriteLine("Data after remove:");
            foreach (var e in readPart.Iterate())
            {
                Console.WriteLine($"{e.Key}: {e.Value.Length}");
            }
        }

        private static void FullStoreTest(string root)
        {
            var r = new Random(Environment.TickCount);
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions { Enabled = true, FileIndexCount = 64 },
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 128).ToString()),
                MergeFunction = list =>
                {
                    ulong s = 0;
                    return Compressor.GetEncodedBytes(list.OrderBy(c => c), ref s);
                },
                Partitions = new List<StoreCatalogPartition<Metadata>>
                {
                    new StoreCatalogPartition<Metadata>("Date", m => m.Date.ToString("yyyyMMdd"))
                },
                KeyComparer = (left, right) => left == right ? 0 : (left < right) ? -1 : 1,
            };
            FSUtils.CleanAndTestFolder(root);

            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var storePart = store.CreateBuilder(new Metadata { Date = new DateTime(2022, 11, 08) });
                       
            Log.Info("Fill start");
            for (int i = 0; i < 30000000; i++)
            {
                var s = Generate(r);
                var count = r.Next(200);
                for (int j = 0; j < count; j++)
                {
                    var t = Generate(r);
                    storePart.Store(s, t);
                }
            }
            storePart.CompleteAdding();
            Log.Info("Fill complete");

            long cnt = 0;
            foreach (var p in storePart.Iterate())
            {
                if (p.Key % 2 == 0) cnt++;
            }
            Log.Info(cnt.ToString());

            Log.Info("Fill test complete");

            storePart.Compress();
            Log.Info("Compress complete");

            var reader = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            cnt = 0;
            foreach (var p in reader.Iterate())
            {
                if (p.Key % 2 == 0) cnt++;
            }
            Log.Info(cnt.ToString());
            Log.Info("Compress test complete");

            storePart.DropData();

            Log.Info("Complete#1");
            Console.ReadKey();

            FSUtils.CleanAndTestFolder(root);
            

            var sw = new Stopwatch();
            sw.Start();

            var testKeys1 = new List<ulong>();
            var testKeys2 = new List<ulong>();

            var testData = new Dictionary<ulong, HashSet<ulong>>();

            var total = 0L;

            for (int i = 0; i < 2000000; i++)
            {
                var s = Generate(r);
                if (testData.ContainsKey(s) == false) testData[s] = new HashSet<ulong>();
                var count = r.Next(300);
                total++;
                for (int j = 0; j < count; j++)
                {
                    total++;
                    var t = Generate(r);
                    storePart.Store(s, t);
                    testData[s].Add(t);
                }
                if (s % 177 == 0)
                {
                    testKeys1.Add(s);
                }
                if (s % 223 == 0)
                {
                    testKeys2.Add(s);
                }
            }

            sw.Stop();
            Log.Info($"Fill journal: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart.CompleteAdding();
            storePart.Compress();
            sw.Stop();
            Log.Info($"Compress: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart.RebuildIndex();
            sw.Stop();
            Log.Info($"Rebuild indexes: {sw.ElapsedMilliseconds}ms");

            Log.Info("Start merge test");
            sw.Restart();
            var merger = store.CreateMergeAccessor(new Metadata { Date = new DateTime(2022, 11, 08) }, data => Compressor.DecodeBytesContent(data));
            for (int i = 0; i < 2300000; i++)
            {
                total++;
                var s = Generate(r);
                if (testData.ContainsKey(s) == false) testData[s] = new HashSet<ulong>();
                var count = r.Next(300);
                for (int j = 0; j < count; j++)
                {
                    total++;
                    var t = Generate(r);
                    merger.Store(s, t);
                    testData[s].Add(t);
                }
            }
            Log.Info($"Merge journal filled: {sw.ElapsedMilliseconds}ms. Total data count: {total}");
            merger.Compress(); // auto reindex
            sw.Stop();
            Log.Info($"Compress after merge: {sw.ElapsedMilliseconds}ms");

            Log.Info("Test #1 reading");
            var readPart = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            ulong totalData = 0;
            ulong totalKeys = 0;
            foreach (var key in testKeys1)
            {
                var result = readPart.Find(key);
                totalData += (ulong)(result.Value?.Length ?? 0);
                totalKeys++;
            }
            Log.Info($"\t\tFound: {totalKeys} keys. {totalData} bytes");
            totalData = 0;
            totalKeys = 0;
            Log.Info("Test #1 remove by keys");
            for (int i = 0; i < testKeys1.Count; i++)
            {
                readPart.RemoveKey(testKeys1[i], false);
            }
            sw.Restart();
            readPart.RebuildIndex();
            sw.Stop();
            Log.Info($"Rebuild indexes after remove: {sw.ElapsedMilliseconds}ms");
            Log.Info("Test #1 reading after remove");
            foreach (var key in testKeys1)
            {
                var result = readPart.Find(key);
                totalData += (ulong)(result.Value?.Length ?? 0);
                totalKeys++;
            }
            Log.Info($"\t\tFound: {totalKeys} keys. {totalData} bytes");
            totalData = 0;
            totalKeys = 0;
            Log.Info("Test #2 reading");
            foreach (var key in testKeys2)
            {
                var result = readPart.Find(key);
                totalData += (ulong)(result.Value?.Length ?? 0);
                totalKeys++;
            }
            Log.Info($"\t\tFound: {totalKeys} keys. {totalData} bytes");
            totalData = 0;
            totalKeys = 0;
            Log.Info("Test #2 remove keys batch");
            readPart.RemoveKeys(testKeys2);
            Log.Info("Test #2 reading after remove");
            foreach (var key in testKeys2)
            {
                var result = readPart.Find(key);
                totalData += (ulong)(result.Value?.Length ?? 0);
                totalKeys++;
            }
            Log.Info($"\t\tFound: {totalKeys} keys. {totalData} bytes");
            totalData = 0;
            totalKeys = 0;

            Log.Info("Iterator test");
            foreach (var e in readPart.Iterate())
            {
                totalData += (ulong)(e.Value?.Length ?? 0);
                totalKeys++;
            }
            Log.Info($"\t\tFound: {totalKeys} keys. {totalData} bytes");
            totalData = 0;
            totalKeys = 0;
            Log.Info("Test stored data");

            foreach (var test in testData)
            {
                if (test.Value.Count > 0 && testKeys1.Contains(test.Key) == false && testKeys2.Contains(test.Key) == false)
                {
                    var result = Compressor.DecodeBytesContent(readPart.Find(test.Key).Value).ToHashSet();
                    if (test.Value.Count != result.Count)
                    {
                        Log.Info($"Key '{test.Key}' not found!");
                        continue;
                    }
                    foreach (var t in test.Value)
                    {
                        if (result.Contains(t) == false)
                        {
                            Log.Info($"Value '{t}' from test data missed in base");
                        }
                    }
                }
            }
            Log.Info("Completed");
        }


        static void Main(string[] args)
        {
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug);
            var root = @"H:\temp";
            //FastTest(root);
            FullStoreTest(root);
            //TestIterations(root);
            //TestRangeCompressionAndInversion();
            Console.ReadKey();
        }

        private static void TestRangeCompressionAndInversion()
        {
            var list = new List<FilePositionRange>();
            list.Add(new FilePositionRange { Start = 5, End = 12 });
            list.Add(new FilePositionRange { Start = 16, End = 21 });
            RangeCompression(list);
            foreach (var r in list)
            {
                Console.WriteLine($"{r.Start}: {r.End}");
            }
            Console.WriteLine("Invert ranges");
            var inverted = RangeInversion(list, 21);
            foreach (var r in inverted)
            {
                Console.WriteLine($"{r.Start}: {r.End}");
            }
        }

        private static void RangeCompression(List<FilePositionRange> ranges)
        {
            for (var i = 0; i < ranges.Count - 1; i++)
            {
                var current = ranges[i];
                var next = ranges[i + 1];
                if (current.End == next.Start)
                {
                    current.End = next.End;
                    ranges.RemoveAt(i + 1);
                    i--;
                }
            }
        }

        private static List<FilePositionRange> RangeInversion(List<FilePositionRange> ranges, long length)
        {
            if ((ranges?.Count ?? 0) == 0) return new List<FilePositionRange> { new FilePositionRange { Start = 0, End = length } };
            var inverted = new List<FilePositionRange>();
            var current = new FilePositionRange { Start = 0, End = ranges[0].Start };
            for (var i = 0; i < ranges.Count; i++)
            {
                current.End = ranges[i].Start;
                if (current.Start != current.End)
                {
                    inverted.Add(new FilePositionRange { Start = current.Start, End = current.End });
                }
                current.Start = ranges[i].End;
            }
            if (current.End != length)
            {
                if (current.Start != length)
                {
                    inverted.Add(new FilePositionRange { Start = current.Start, End = length });
                }
            }
            return inverted;
        }
    }
}