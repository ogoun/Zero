using ZeroLevel;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Storages;

namespace PartitionFileStorageTest
{
    internal class Program
    {
        public class PartitionKey
        {
            public DateTime Date { get; set; }
            public ulong Ctn { get; set; }
        }

        public class Record
            : IBinarySerializable
        {
            public string[] Hosts { get; set; }

            public void Deserialize(IBinaryReader reader)
            {
                this.Hosts = reader.ReadStringArray();
            }

            public void Serialize(IBinaryWriter writer)
            {
                writer.WriteArray(this.Hosts);
            }
        }

        static Record GenerateRecord()
        {
            var record = new Record();
            var rnd = new Random((int)Environment.TickCount);
            var count = rnd.Next(400);
            record.Hosts = new string[count];
            for (int i = 0; i < count; i++)
            {
                record.Hosts[i] = Guid.NewGuid().ToString();
            }
            return record;
        }

        static PartitionKey GenerateKey()
        {
            var key = new PartitionKey();
            var rnd = new Random((int)Environment.TickCount);
            key.Ctn = (ulong)rnd.Next(1000);
            key.Date = DateTime.Now.AddDays(-rnd.Next(30)).Date;
            return key;
        }

        class DataConverter
            : IPartitionDataConverter<Record>
        {
            public IEnumerable<Record> ConvertFromStorage(Stream stream)
            {
                var reader = new MemoryStreamReader(stream);
                while (reader.EOS == false)
                {
                    yield return reader.Read<Record>();
                }
            }

            public void ConvertToStorage(Stream stream, IEnumerable<Record> data)
            {
                var writer = new MemoryStreamWriter(stream);
                foreach (var record in data)
                {
                    writer.Write<Record>(record);
                }
            }
        }

        private static int COUNT_NUMBERS = ulong.MaxValue.ToString().Length;
        static void Main(string[] args)
        {
            var testDict = new Dictionary<ulong, Dictionary<DateTime, List<Record>>>();
            var options = new PartitionFileSystemStorageOptions<PartitionKey, Record>
            {
                MaxDegreeOfParallelism = 1,
                DataConverter = new DataConverter(),
                UseCompression = true,
                MergeFiles = false,
                RootFolder = Path.Combine(Configuration.BaseDirectory, "root")
            };
            options.Partitions.Add(new Partition<PartitionKey>("data", p => p.Date.ToString("yyyyMMdd")));
            options.Partitions.Add(new Partition<PartitionKey>("ctn", p => p.Ctn.ToString().PadLeft(COUNT_NUMBERS, '0')));
            var storage = new PartitionFileSystemStorage<PartitionKey, Record>(options);

            for (int i = 0; i < 50000; i++)
            {
                if (i % 100 == 0)
                    Console.WriteLine(i);
                var key = GenerateKey();
                var record = GenerateRecord();
                if (testDict.ContainsKey(key.Ctn) == false)
                {
                    testDict[key.Ctn] = new Dictionary<DateTime, List<Record>>();
                }
                if (testDict[key.Ctn].ContainsKey(key.Date) == false)
                {
                    testDict[key.Ctn][key.Date] = new List<Record>();
                }
                testDict[key.Ctn][key.Date].Add(record);
                storage.WriteAsync(key, new[] { record }).Wait();
            }
            foreach (var cpair in testDict)
            {
                foreach (var dpair in cpair.Value)
                {
                    var key = new PartitionKey { Ctn = cpair.Key, Date = dpair.Key };
                    var data = storage.CollectAsync(new[] { key }).Result.ToArray();
                    var testData = dpair.Value;

                    if (data.Length != testData.Count)
                    {
                        Console.WriteLine($"[{key.Date.ToString("yyyyMMdd")}] {key.Ctn} Wrong count. Expected: {testData.Count}. Got: {data.Length}");
                    }
                    else
                    {
                        var datahosts = data.SelectMany(r => r.Hosts).OrderBy(s => s).ToArray();
                        var testhosts = testData.SelectMany(r => r.Hosts).OrderBy(s => s).ToArray();
                        if (datahosts.Length != testhosts.Length)
                        {
                            Console.WriteLine($"[{key.Date.ToString("yyyyMMdd")}] {key.Ctn}. Records not equals. Different hosts count");
                        }
                        for (int i = 0; i < datahosts.Length; i++)
                        {
                            if (string.Compare(datahosts[i], testhosts[i], StringComparison.Ordinal) != 0)
                            {
                                Console.WriteLine($"[{key.Date.ToString("yyyyMMdd")}] {key.Ctn}. Records not equals. Different hosts");
                            }
                        }
                    }
                }
            }
        }
    }
}