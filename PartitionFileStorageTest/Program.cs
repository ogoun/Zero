using System.Collections.Concurrent;
using System.Diagnostics;
using ZeroLevel;
using ZeroLevel.Collections;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.Memory;
using ZeroLevel.Services.PartitionStorage;
using ZeroLevel.Services.Serialization;

namespace PartitionFileStorageTest
{
    public class StoreMetadata
    {
        public DateTime Date { get; set; }
    }
    internal class Program
    {
        //        const int PAIRS_COUNT = 200_000_000;
        const long PAIRS_COUNT = 10_000_000;

        private class Metadata
        {
            public DateTime Date { get; set; }
        }

        const ulong num_base = 79770000000;

        private static ulong Generate(Random r)
        {
            return num_base + (uint)r.Next(999999);
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
            store.Dispose();
            Log.Info("Completed");
        }

        private static IEnumerable<(ulong, ulong)> MassGenerator(long count)
        {
            var r = new Random(Environment.TickCount);
            for (long i = 0; i < count; i++)
            {
                yield return (Generate(r), Generate(r));
            }
        }

        private static void FullStoreMultithreadTest(StoreOptions<ulong, ulong, byte[], Metadata> options)
        {
            var meta = new Metadata { Date = new DateTime(2022, 11, 08) };
            var r = new Random(Environment.TickCount);
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var storePart = store.CreateBuilder(meta);
            var sw = new Stopwatch();
            sw.Start();
            var insertCount = (long)(0.7 * PAIRS_COUNT);
            var testKeys1 = new ConcurrentBag<ulong>();
            var testKeys2 = new ConcurrentBag<ulong>();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 24 };
            var Keys = new ConcurrentHashSet<ulong>();

            Parallel.ForEach(MassGenerator((long)(0.7 * PAIRS_COUNT)), parallelOptions, pair =>
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
                Keys.Add(key);
            });

            if (storePart.TotalRecords != insertCount)
            {
                Log.Error($"Count of stored record no equals expected. Recorded: {storePart.TotalRecords}. Expected: {insertCount}. Unique keys: {Keys.Count}");
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

            Parallel.ForEach(MassGenerator((long)(0.3 * PAIRS_COUNT)), parallelOptions, pair =>
            {
                var key = pair.Item1;
                var val = pair.Item2;
                merger.Store(key, val);
                Keys.Add(key);
            });

            if (merger.TotalRecords != ((long)(0.3 * PAIRS_COUNT)))
            {
                Log.Error($"Count of stored record no equals expected. Recorded: {merger.TotalRecords}. Expected: {((long)(0.3 * PAIRS_COUNT))}");
            }

            Log.Info($"Merge journal filled: {sw.ElapsedMilliseconds}ms. Total data count: {PAIRS_COUNT}. Unique keys: {Keys.Count}");
            merger.Compress(); // auto reindex
            sw.Stop();
            Log.Info($"Compress after merge: {sw.ElapsedMilliseconds}ms");

            ulong totalData = 0;
            ulong totalKeys = 0;

            var s = new HashSet<ulong>();
            store.Bypass(meta, (k, v) => 
            {
                s.Add(k);
            });
            Log.Info($"Keys count: {s.Count}");

            using (var readPart = store.CreateAccessor(meta))
            {
                /*
                Log.Info("Test #1 reading");
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
                */
                Log.Info("Test #2 reading");
                foreach (var key in testKeys2)
                {
                    var result = readPart.Find(key);
                    totalData += (ulong)(result.Value?.Length ?? 0);
                    totalKeys++;
                }
                Log.Info($"\t\tFound: {totalKeys}/{Keys.Count} keys. {totalData} bytes");
                totalData = 0;
                totalKeys = 0;
                Log.Info("Test #2 remove keys batch");
                readPart.RemoveKeys(testKeys2);
                foreach (var k in testKeys2)
                {
                    Keys.TryRemove(k);
                }
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
            }
            Log.Info($"\t\tFound: {totalKeys}/{Keys.Count} keys. {totalData} bytes");
            store.Dispose();
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
                    serializer.KeyDeserializer.Invoke(reader, out var k);
                    serializer.ValueDeserializer.Invoke(reader, out var _);
                    if (counter == 0)
                    {
                        index[k] = pos;
                        counter = 16;
                    }
                }
            }

            // 2 Test index
            var fileReader = new ParallelFileReader(filePath);
            foreach (var pair in index)
            {
                var accessor = fileReader.GetAccessor(pair.Value);
                using (var reader = new MemoryStreamReader(accessor))
                {
                    serializer.KeyDeserializer.Invoke(reader, out var k);
                    if (k != pair.Key)
                    {
                        Log.Warning("Broken index");
                    }
                    serializer.ValueDeserializer.Invoke(reader, out var _);
                }
            }


            /*
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
            */
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
                        serializer.KeyDeserializer.Invoke(reader, out var key);
                        if (false == dict.ContainsKey(key))
                        {
                            dict[key] = new HashSet<ulong>();
                        }
                        if (reader.EOS)
                        {
                            break;
                        }
                        serializer.InputDeserializer.Invoke(reader, out var input);
                        dict[key].Add(input);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "FaultUncompressedReadTest");
                    }
                }
            }
        }

        private static void FaultCompressionTest(string folder, StoreMetadata meta)
        {
            var options = new StoreOptions<string, ulong, byte[], StoreMetadata>
            {
                Index = new IndexOptions
                {
                    Enabled = true,
                    StepType = IndexStepType.Step,
                    StepValue = 32,
                    EnableIndexInMemoryCachee = true
                },
                RootFolder = folder,
                FilePartition = new StoreFilePartition<string, StoreMetadata>("Host hash", (key, _) => Math.Abs(StringHash.DotNetFullHash(key) % 367).ToString()),
                MergeFunction = list =>
                {
                    ulong s = 0;
                    return Compressor.GetEncodedBytes(list.OrderBy(c => c), ref s);
                },
                Partitions = new List<StoreCatalogPartition<StoreMetadata>>
                {
                    new StoreCatalogPartition<StoreMetadata>("Date", m => m.Date.ToString("yyyyMMdd")),
                },
                KeyComparer = (left, right) => string.Compare(left, right, true),
                ThreadSafeWriting = true
            };
            var store = new Store<string, ulong, byte[], StoreMetadata>(options);
            var builder = store.CreateBuilder(meta);
            builder.Compress();
        }

        static void Main(string[] args)
        {
            //FaultCompressionTest(@"F:\Desktop\DATKA\DNS", new StoreMetadata { Date = new DateTime(2023, 01, 20) });

            var root = @"H:\temp";
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions
                {
                    Enabled = true,
                    StepType = IndexStepType.Step,
                    StepValue = 32,
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
                    StepValue = 32,
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
                ThreadSafeWriting = true,
                MaxDegreeOfParallelism = 16
            };
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug);
            /*
            var pairs = new List<(ulong, ulong)>(PAIRS_COUNT);
            var r = new Random(Environment.TickCount);
            Log.Info("Start create dataset");
            for (int i = 0; i < PAIRS_COUNT; i++)
            {
                pairs.Add((Generate(r), Generate(r)));
            }
            */
            Log.Info("Start test");
            // FastTest(options);
            FSUtils.CleanAndTestFolder(root);
            FullStoreMultithreadTest(optionsMultiThread);


            /*  
          FSUtils.CleanAndTestFolder(root);
          FullStoreTest(options, pairs);
            */
            //TestParallelFileReadingMMF();

            /*
            
            FSUtils.CleanAndTestFolder(root);
            
            */
            Console.ReadKey();
        }


        static void TestParallelFileReading()
        {
            var path = @"C:\Users\Ogoun\Downloads\Lego_super_hero.iso";
            var threads = new List<Thread>();
            for (int i = 0; i < 100; i++)
            {
                var k = i;
                var reader = GetReadStream(path);
                var t = new Thread(() => PartReader(reader, 1000000 + k * 1000));
                t.IsBackground = true;
                threads.Add(t);
            }
            foreach (var t in threads)
            {
                t.Start();
            }
        }

        static void TestParallelFileReadingMMF()
        {
            var filereader = new ParallelFileReader(@"C:\Users\Ogoun\Downloads\Lego_super_hero.iso");
            var threads = new List<Thread>();
            for (int i = 0; i < 100; i++)
            {
                var k = i;
                var t = new Thread(() => PartReaderMMF(filereader.GetAccessor(1000000 + k * 1000)));
                t.IsBackground = true;
                threads.Add(t);
            }

            foreach (var t in threads)
            {
                t.Start();
            }

        }

        static MemoryStreamReader GetReadStream(string filePath)
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
            return new MemoryStreamReader(stream);
        }

        static void PartReader(MemoryStreamReader reader, long offset)
        {
            var count = 0;
            using (reader)
            {
                reader.SetPosition(offset);
                for (int i = 0; i < 1000000; i++)
                {
                    if (reader.ReadByte() == 127) count++;
                }
            }
            Console.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId}: {count}");
        }

        static void PartReaderMMF(IViewAccessor accessor)
        {
            var count = 0;
            var lastPosition = accessor.Position;
            using (var reader = new MemoryStreamReader(accessor))
            {
                for (int i = 0; i < 1000000; i++)
                {
                    if (reader.ReadByte() == 127) count++;
                    if (lastPosition > reader.Position)
                    {
                        // Test for correct absolute position
                        Console.WriteLine("Fock!");
                    }
                    lastPosition = reader.Position;
                }
            }
            Console.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId}: {count}");
        }
    }
}