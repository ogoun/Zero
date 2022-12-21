using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ZeroLevel;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage;
using ZeroLevel.Services.Serialization;

namespace PartitionFileStorageTest
{
    internal class Program
    {
        //        const int PAIRS_COUNT = 200_000_000;
        const int PAIRS_COUNT = 2000_000;

        private class Metadata
        {
            public DateTime Date { get; set; }
        }

        private static ulong Generate(Random r)
        {
            var num = new StringBuilder();
            num.Append("79");
            num.Append(r.Next(99).ToString("D2"));
            num.Append(r.Next(9999).ToString("D7"));
            return ulong.Parse(num.ToString());
        }

        private static void FastTest(StoreOptions<ulong, ulong, byte[], Metadata> options)
        {
            var r = new Random(Environment.TickCount);
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

        private static void FullStoreTest(StoreOptions<ulong, ulong, byte[], Metadata> options,
            List<(ulong, ulong)> pairs)
        {
            var meta = new Metadata { Date = new DateTime(2022, 11, 08) };
            var r = new Random(Environment.TickCount);
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var storePart = store.CreateBuilder(meta);
            var sw = new Stopwatch();
            sw.Start();
            var testKeys1 = new List<ulong>();
            var testKeys2 = new List<ulong>();
            var testData = new Dictionary<ulong, HashSet<ulong>>();

            var insertCount = (int)(0.7 * pairs.Count);

            for (int i = 0; i < insertCount; i++)
            {
                var key = pairs[i].Item1;
                var val = pairs[i].Item2;
                if (testData.ContainsKey(key) == false) testData[key] = new HashSet<ulong>();
                testData[key].Add(val);
                storePart.Store(key, val);
                if (key % 717 == 0)
                {
                    testKeys1.Add(key);
                }
                if (key % 117 == 0)
                {
                    testKeys2.Add(key);
                }
            }

            sw.Stop();
            Log.Info($"Fill journal: {sw.ElapsedMilliseconds}ms. Records writed: {storePart.TotalRecords}");
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
            var merger = store.CreateMergeAccessor(meta, data => Compressor.DecodeBytesContent(data));
            for (int i = insertCount; i < pairs.Count; i++)
            {
                var key = pairs[i].Item1;
                var val = pairs[i].Item2;
                if (testData.ContainsKey(key) == false) testData[key] = new HashSet<ulong>();
                testData[key].Add(val);
                merger.Store(key, val);
            }
            Log.Info($"Merge journal filled: {sw.ElapsedMilliseconds}ms. New records merged: {merger.TotalRecords}");
            merger.Compress(); // auto reindex
            sw.Stop();
            Log.Info($"Compress after merge: {sw.ElapsedMilliseconds}ms");

            Log.Info("Test #1 reading");
            var readPart = store.CreateAccessor(meta);
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

        private static void FullStoreMultithreadTest(StoreOptions<ulong, ulong, byte[], Metadata> options,
            List<(ulong, ulong)> pairs)
        {
            var meta = new Metadata { Date = new DateTime(2022, 11, 08) };
            var r = new Random(Environment.TickCount);
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var storePart = store.CreateBuilder(meta);
            var sw = new Stopwatch();
            sw.Start();
            var insertCount = (int)(0.7 * pairs.Count);
            var testKeys1 = new ConcurrentBag<ulong>();
            var testKeys2 = new ConcurrentBag<ulong>();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 24 };

            Parallel.ForEach(pairs.Take(insertCount).ToArray(), parallelOptions, pair =>
            {
                var key = pair.Item1;
                var val = pair.Item2;
                storePart.Store(key, val);
                if (key % 717 == 0)
                {
                    testKeys1.Add(key);
                }
                if (key % 117 == 0)
                {
                    testKeys2.Add(key);
                }
            });

            if (storePart.TotalRecords != insertCount)
            {
                Log.Error($"Count of stored record no equals expected. Recorded: {storePart.TotalRecords}. Expected: {insertCount}");
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
            var merger = store.CreateMergeAccessor(meta, data => Compressor.DecodeBytesContent(data));

            Parallel.ForEach(pairs.Skip(insertCount).ToArray(), parallelOptions, pair =>
            {
                var key = pair.Item1;
                var val = pair.Item2;
                merger.Store(key, val);
            });

            if (merger.TotalRecords != (pairs.Count - insertCount))
            {
                Log.Error($"Count of stored record no equals expected. Recorded: {merger.TotalRecords}. Expected: {(pairs.Count - insertCount)}");
            }

            Log.Info($"Merge journal filled: {sw.ElapsedMilliseconds}ms. Total data count: {pairs.Count}");
            merger.Compress(); // auto reindex
            sw.Stop();
            Log.Info($"Compress after merge: {sw.ElapsedMilliseconds}ms");

            Log.Info("Test #1 reading");
            var readPart = store.CreateAccessor(meta);
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
            foreach (var key in testKeys1)
            {
                readPart.RemoveKey(key, false);
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
            Log.Info("Completed");
        }

        private static void FaultIndexTest(string filePath)
        {
            var serializer = new StoreStandartSerializer<ulong, ulong, byte[]>();
            // 1 build index
            var index = new Dictionary<ulong, long>();
            using (var reader = new MemoryStreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024)))
            {
                var counter = 1;
                while (reader.EOS == false)
                {
                    counter--;
                    var pos = reader.Position;
                    var k = serializer.KeyDeserializer.Invoke(reader);
                    serializer.ValueDeserializer.Invoke(reader);
                    if (counter == 0)
                    {
                        index[k] = pos;
                        counter = 16;
                    }
                }
            }

            // 2 Test index
            using (var reader = new MemoryStreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024)))
            {
                foreach (var pair in index)
                {
                    reader.Stream.Seek(pair.Value, SeekOrigin.Begin);
                    var k = serializer.KeyDeserializer.Invoke(reader);
                    if (k != pair.Key)
                    {
                        Log.Warning("Broken index");
                    }
                    var v = serializer.ValueDeserializer.Invoke(reader);
                }
            }
        }

        private static void FaultUncompressedReadTest(string filePath)
        {
            var serializer = new StoreStandartSerializer<ulong, ulong, byte[]>();
            // 1 build index
            var dict = new Dictionary<ulong, HashSet<ulong>>();
            using (var reader = new MemoryStreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096 * 1024)))
            {
                while (reader.EOS == false)
                {
                    try
                    {
                        var key = serializer.KeyDeserializer.Invoke(reader);
                        if (false == dict.ContainsKey(key))
                        {
                            dict[key] = new HashSet<ulong>();
                        }
                        if (reader.EOS)
                        {
                            break;
                        }
                        var input = serializer.InputDeserializer.Invoke(reader);
                        dict[key].Add(input);
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            /*FaultIndexTest(@"H:\temp\85");
            return;*/
            /*
            FaultUncompressedReadTest(@"H:\temp\107");
            return;
            */

            var root = @"H:\temp";
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions
                {
                    Enabled = true,
                    StepType = IndexStepType.Step,
                    StepValue = 16,
                    EnableIndexInMemoryCachee = true
                },
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
                ThreadSafeWriting = false
            };
            var optionsMultiThread = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions
                {
                    Enabled = true,
                    StepType = IndexStepType.Step,
                    StepValue = 16,
                    EnableIndexInMemoryCachee = true
                },
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
                ThreadSafeWriting = true
            };
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug);
            Log.Info("Start");

            var pairs = new List<(ulong, ulong)>(PAIRS_COUNT);
            var r = new Random(Environment.TickCount);
            for (int i = 0; i < PAIRS_COUNT; i++)
            {
                pairs.Add((Generate(r), Generate(r)));
            }

            // FastTest(options);
           /* FSUtils.CleanAndTestFolder(root);
            FullStoreMultithreadTest(optionsMultiThread, pairs);*/

            FSUtils.CleanAndTestFolder(root);
            FullStoreTest(options, pairs);

            /*
            
            FSUtils.CleanAndTestFolder(root);
            
            */
            Console.ReadKey();
        }
    }
}