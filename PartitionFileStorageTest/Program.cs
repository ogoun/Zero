using System.Diagnostics;
using System.Text;
using ZeroLevel.Services.PartitionStorage;
using ZeroLevel.Services.Serialization;

namespace PartitionFileStorageTest
{
    public class CallRecordParser
    {
        private static HashSet<char> _partsOfNumbers = new HashSet<char> { '*', '#', '+', '(', ')', '-' };
        private StringBuilder sb = new StringBuilder();
        private const string NO_VAL = null;

        private string ReadNumber(string line)
        {
            sb.Clear();
            var started = false;
            foreach (var ch in line)
            {
                if (char.IsDigit(ch))
                {
                    if (started)
                    {
                        sb.Append(ch);
                    }
                    else if (ch != '0')
                    {
                        sb.Append(ch);
                        started = true;
                    }
                }
                else if (char.IsWhiteSpace(ch) || _partsOfNumbers.Contains(ch)) continue;
                else return NO_VAL;
            }
            if (sb.Length == 11 && sb[0] == '8') sb[0] = '7';
            if (sb.Length == 3 || sb.Length == 4 || sb.Length > 10)
                return sb.ToString();
            return NO_VAL;
        }
        private HashSet<string> ReadNumbers(string line)
        {
            var result = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(line) == false)
            {
                char STX = (char)0x0002;
                var values = line.Split(STX);
                if (values.Length > 0)
                {
                    foreach (var val in values)
                    {
                        var number = ReadNumber(val);
                        if (number != null)
                        {
                            result.Add(number);
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Парсинг строки исходного файла
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public CallRecord Parse(string line)
        {
            var parts = line.Split('\t');

            if (parts.Length != 2) return null;

            var msisdn = ReadNumber(parts[0].Trim());

            if (string.IsNullOrWhiteSpace(msisdn) == false)
            {
                var numbers = ReadNumbers(parts[1]);
                if (numbers != null && numbers.Count > 0)
                {
                    return new CallRecord
                    {
                        Msisdn = msisdn,
                        Msisdns = numbers
                    };
                }
            }
            return null;
        }
    }

    public class CallRecord
    {
        public string Msisdn;
        public HashSet<string> Msisdns;
    }
    internal class Program
    {
        private class Metadata
        {
            public DateTime Date { get; set; }
            public bool Incoming { get; set; }
        }

        private static void BuildStore(string source, string root)
        {
            var options = new IStoreOptions<ulong, ulong, byte[], Metadata>
            {
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 1000).ToString()),
                MergeFunction = list =>
                {
                    ulong s = 0;
                    return Compressor.GetEncodedBytes(list.OrderBy(c => c), ref s);
                },
                Partitions = new List<StoreCatalogPartition<Metadata>>
                {
                    new StoreCatalogPartition<Metadata>("Date", m => m.Date.ToString("yyyyMMdd")),
                    new StoreCatalogPartition<Metadata>("Date", m => m.Incoming ? "incoming" : "outcoming")
                },
                KeyComparer = (left, right) => left == right ? 0 : (left < right) ? 1 : -1
            };
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            
            
            var storeIncoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08), Incoming = true });
            var storeOutcoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08), Incoming = false });
            var parser = new CallRecordParser();
            var sw = new Stopwatch();
            sw.Start();
            using (FileStream fs = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (BufferedStream bs = new BufferedStream(fs, 1024 * 1024 * 64))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var record = parser.Parse(line);
                            if (record == null) continue;
                            if (!string.IsNullOrWhiteSpace(record?.Msisdn ?? string.Empty) && ulong.TryParse(record.Msisdn, out var n))
                            {
                                var ctns = record.Msisdns.ParseMsisdns().ToArray();
                                foreach (var ctn in ctns)
                                {
                                    storeIncoming.Store(n, ctn);
                                    storeOutcoming.Store(ctn, n);
                                }
                            }
                        }
                    }
                }
            }
            sw.Stop();
            Console.WriteLine($"Fill journal: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            storeIncoming.CompleteStoreAndRebuild();
            storeOutcoming.CompleteStoreAndRebuild();
            sw.Stop();
            Console.WriteLine($"Rebuild journal to store: {sw.ElapsedMilliseconds}ms");
        }

        private static void TestReading(string source, string root)
        {
            var options = new IStoreOptions<ulong, ulong, byte[], Metadata>
            {
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("Last three digits", (ctn, date) => (ctn % 1000).ToString()),
                MergeFunction = list =>
                {
                    ulong s = 0;
                    return Compressor.GetEncodedBytes(list.OrderBy(c => c), ref s);
                },
                Partitions = new List<StoreCatalogPartition<Metadata>>
                {
                    new StoreCatalogPartition<Metadata>("timestamp", m => m.Date.ToString("yyyyMMdd")),
                    new StoreCatalogPartition<Metadata>("timestamp", m => m.Incoming ? "incoming" : "outcoming")
                },
                KeyComparer = (left, right) => left == right ? 0 : (left < right) ? -1 : 1
            };
            var store = new Store<ulong, ulong, byte[], Metadata>(options);
            var request = new StoreSearchRequest<ulong, Metadata>
            {
                PartitionSearchRequests = new List<PartitionSearchRequest<ulong, Metadata>>
                {
                    new PartitionSearchRequest<ulong, Metadata>
                    {
                        Info = new Metadata { Date = new DateTime(2022, 11, 08), Incoming = true },
                        Keys = new ulong[] { 79645090604 }
                    },
                    new PartitionSearchRequest<ulong, Metadata>
                    {
                        Info = new Metadata { Date = new DateTime(2022, 11, 08), Incoming = false },
                        Keys = new ulong[] { 79645090604 }
                    }
                }
            };

            var storeIncoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08), Incoming = true });
            Console.WriteLine($"Incoming data files: {storeIncoming.CountDataFiles()}");
            var storeOutcoming = store.CreateAccessor(new Metadata { Date = new DateTime(2022, 11, 08), Incoming = false });
            Console.WriteLine($"Outcoming data files: {storeOutcoming.CountDataFiles()}");
            var sw = new Stopwatch();
            sw.Start();
            var result = store.Search(request).Result;
            foreach (var r in result.Results)
            {
                Console.WriteLine($"Incoming: {r.Key.Incoming}");
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

        static void Main(string[] args)
        {
            var root = @"H:\temp";
            var source = @"H:\319a9c31-d823-4dd1-89b0-7fb1bb9c4859.txt";
            BuildStore(source, root);
            TestReading(source, root);
            Console.ReadKey();
        }
    }
}