using System.Diagnostics;
using System.Text;
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

        private static void BuildStore(string root)
        {
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
            var storePart1 = store.CreateBuilder(new Metadata { Date = new DateTime(2022, 11, 08) });
            var storePart2 = store.CreateBuilder(new Metadata { Date = new DateTime(2022, 11, 09) });

            var sw = new Stopwatch();
            sw.Start();

            var r = new Random(Environment.TickCount);
            for (int i = 0; i < 1000000; i++)
            {
                var s = Generate(r);
                var count = r.Next(300);
                for (int j = 0; j < count; j++)
                {
                    var t = Generate(r);
                    storePart1.Store(s, t);
                }
            }
            for (int i = 0; i < 1000000; i++)
            {
                var s = Generate(r);
                var count = r.Next(300);
                for (int j = 0; j < count; j++)
                {
                    var t = Generate(r);
                    storePart2.Store(s, t);
                }
            }

            sw.Stop();
            Console.WriteLine($"Fill journal: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart1.CompleteAddingAndCompress();
            storePart2.CompleteAddingAndCompress();
            sw.Stop();
            Console.WriteLine($"Rebuild journal to store: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart1.RebuildIndex();
            storePart2.RebuildIndex();
            sw.Stop();
            Console.WriteLine($"Rebuild indexes: {sw.ElapsedMilliseconds}ms");
        }

        private static void SmallFullTest(string root)
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
            storePart.CompleteAddingAndCompress();
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

        private static void TestBuildRemoveStore(string root)
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

            var sw = new Stopwatch();
            sw.Start();

            var testKeys1 = new List<ulong>();
            var testKeys2 = new List<ulong>();

            for (int i = 0; i < 1000000; i++)
            {
                var s = Generate(r);
                var count = r.Next(300);
                for (int j = 0; j < count; j++)
                {
                    var t = Generate(r);
                    storePart.Store(s, t);
                }
                if (s % 11217 == 0)
                {
                    testKeys1.Add(s);
                }
                if (s % 11219 == 0)
                {
                    testKeys2.Add(s);
                }
            }

            sw.Stop();
            Console.WriteLine($"Fill journal: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart.CompleteAddingAndCompress();
            sw.Stop();
            Console.WriteLine($"Compress: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storePart.RebuildIndex();
            sw.Stop();
            Console.WriteLine($"Rebuild indexes: {sw.ElapsedMilliseconds}ms");

            Console.WriteLine("Start merge test");
            sw.Restart();
            var merger = store.CreateMergeAccessor(new Metadata { Date = new DateTime(2022, 11, 08) }, data => Compressor.DecodeBytesContent(data));
            for (int i = 0; i < 1000000; i++)
            {
                var s = Generate(r);
                var count = r.Next(300);
                for (int j = 0; j < count; j++)
                {
                    var t = Generate(r);
                    merger.Store(s, t);
                }
            }
            Console.WriteLine($"Merge journal filled: {sw.ElapsedMilliseconds}ms");
            merger.CompleteAddingAndCompress();
            sw.Stop();
            Console.WriteLine($"Compress after merge: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            merger.RebuildIndex();
            sw.Stop();
            Console.WriteLine($"Rebuild indexes after merge: {sw.ElapsedMilliseconds}ms");


            Console.WriteLine("Test #1 reading");
            var readPart = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            foreach (var key in testKeys1)
            {
                Console.WriteLine($"\tKey: {key}");
                var result = readPart.Find(key);
                Console.WriteLine($"\t\tFound: {result.Found}. {result.Value?.Length ?? 0} bytes");
            }
            Console.WriteLine("Press to continue");
            Console.ReadKey();
            Console.WriteLine("Test #1 remove by keys");
            for (int i = 0; i < testKeys1.Count; i++)
            {
                if (i % 100 == 0)
                {
                    readPart.RemoveKey(testKeys1[i]);
                }
            }
            Console.WriteLine("Test #1 reading after remove");
            foreach (var key in testKeys1)
            {
                Console.WriteLine($"\tKey: {key}");
                var result = readPart.Find(key);
                Console.WriteLine($"\t\tFound: {result.Found}. {result.Value?.Length ?? 0} bytes");
            }
            Console.WriteLine("Press to continue");
            Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine("---------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Test #2 reading");
            foreach (var key in testKeys2)
            {
                Console.WriteLine($"\tKey: {key}");
                var result = readPart.Find(key);
                Console.WriteLine($"\t\tFound: {result.Found}. {result.Value?.Length ?? 0} bytes");
            }
            Console.WriteLine("Press to continue");
            Console.ReadKey();
            Console.WriteLine("Test #2 remove keys batch");
            readPart.RemoveKeys(testKeys2);
            Console.WriteLine("Test #2 reading after remove");
            foreach (var key in testKeys2)
            {
                Console.WriteLine($"\tKey: {key}");
                var result = readPart.Find(key);
                Console.WriteLine($"\t\tFound: {result.Found}. {result.Value?.Length ?? 0} bytes");
            }

            Console.WriteLine("Press to continue for iteration");
            Console.ReadKey();
            foreach (var e in readPart.Iterate())
            {
                Console.WriteLine($"{e.Key}: {e.Value.Length}");
            }
        }

        private static void TestReading(string root)
        {
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions { Enabled = true, FileIndexCount = 256 },
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 512).ToString()),
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
            var request = new StoreSearchRequest<ulong, Metadata>
            {
                PartitionSearchRequests = new List<PartitionSearchRequest<ulong, Metadata>>
                {
                    new PartitionSearchRequest<ulong, Metadata>
                    {
                        Info = new Metadata { Date = new DateTime(2022, 11, 08) },
                        Keys = new ulong[] {   }
                    },
                    new PartitionSearchRequest<ulong, Metadata>
                    {
                        Info = new Metadata { Date = new DateTime(2022, 11, 09) },
                        Keys = new ulong[] {  }
                    }
                }
            };
            var storeIncoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            Console.WriteLine($"Incoming data files: {storeIncoming.CountDataFiles()}");
            var storeOutcoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 09) });
            Console.WriteLine($"Outcoming data files: {storeOutcoming.CountDataFiles()}");
            var sw = new Stopwatch();
            sw.Start();
            var result = store.Search(request).Result;
            foreach (var r in result.Results)
            {
                foreach (var mr in r.Value)
                {
                    Console.WriteLine($"\tKey: {mr.Key}. Sucess: {mr.Found}");
                    if (mr.Found && mr.Value.Length > 0)
                    {
                        var ctns = Compressor.DecodeBytesContent(mr.Value);
                        Console.WriteLine($"\t\t{string.Join(';', ctns)}");
                    }
                }
            }
            sw.Stop();
            Console.WriteLine($"Search time: {sw.ElapsedMilliseconds}ms");
        }

        private static void TestIterations(string root)
        {
            var options = new StoreOptions<ulong, ulong, byte[], Metadata>
            {
                Index = new IndexOptions { Enabled = true, FileIndexCount = 256 },
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 512).ToString()),
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
            var storeIncoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08) });
            foreach (var r in storeIncoming.Iterate())
            {
                Console.WriteLine($"{r.Key}: {r.Value.Length}");
            }
        }

        static void Main(string[] args)
        {
            var root = @"H:\temp";
            SmallFullTest(root);
            //TestBuildRemoveStore(root);
            //BuildStore(root);
            //TestReading(root);
            //TestIterations(root);
            //TestRangeCompressionAndInversion();
            Console.ReadKey();
        }

        private static void TestRangeCompressionAndInversion()
        {
            var list = new List<FilePositionRange>();
            list.Add(new FilePositionRange { Start = 1, End = 36 });
            list.Add(new FilePositionRange { Start = 36, End = 63 });
            list.Add(new FilePositionRange { Start = 63, End = 89 });
            list.Add(new FilePositionRange { Start = 93, End = 118 });
            list.Add(new FilePositionRange { Start = 126, End = 199 });
            list.Add(new FilePositionRange { Start = 199, End = 216 });
            list.Add(new FilePositionRange { Start = 277, End = 500 });
            RangeCompression(list);
            foreach (var r in list)
            {
                Console.WriteLine($"{r.Start}: {r.End}");
            }
            Console.WriteLine("Invert ranges");
            var inverted = RangeInversion(list, 500);
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