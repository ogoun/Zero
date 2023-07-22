using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests
{
    public sealed class Metadata
    {
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public sealed class TextData
         : IBinarySerializable, IAsyncBinarySerializable
    {
        public string Title { get; set; }
        public string Text { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Title = reader.ReadString();
            this.Text = reader.ReadString();
        }

        public async Task DeserializeAsync(IAsyncBinaryReader reader)
        {
            this.Title = await reader.ReadStringAsync();
            this.Text = await reader.ReadStringAsync();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Title);
            writer.WriteString(this.Text);
        }

        public async Task SerializeAsync(IAsyncBinaryWriter writer)
        {
            await writer.WriteStringAsync(this.Title);
            await writer.WriteStringAsync(this.Text);
        }
    }

    public class FSDBOptions
        : IDisposable
    {
        public StoreOptions<ulong, TextData, TextData[], Metadata> Options { get; private set; }
        public StoreSerializers<ulong, TextData, TextData[]> Serializers { get; private set; }

        public FSDBOptions()
        {
            var root = @"H:\temp";
            FSUtils.CleanAndTestFolder(root);
            // user id, post
            Options = new StoreOptions<ulong, TextData, TextData[], Metadata>
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
                    if (list == null || list.Any() == false)
                    {
                        return new TextData[0];
                    }
                    return list.GroupBy(i => i.Title).Select(pair => new TextData { Title = pair.Key, Text = string.Join(null, pair.Select(p => p.Text)) }).ToArray();
                },
                Partitions = new List<StoreCatalogPartition<Metadata>>
                {
                    new StoreCatalogPartition<Metadata>("Date", m => m.Date),
                    new StoreCatalogPartition<Metadata>("Time", m => m.Time),
                },
                KeyComparer = (left, right) => left == right ? 0 : (left < right) ? -1 : 1,
                ThreadSafeWriting = true,
                MaxDegreeOfParallelism = 16
            };

            Serializers = new StoreSerializers<ulong, TextData, TextData[]>(
                async (w, n) => await w.WriteULongAsync(n),
                async (w, n) => await w.WriteAsync(n),
                async (w, n) => await w.WriteArrayAsync(n),
                async (r) => { try { return new DeserializeResult<ulong>(true, await r.ReadULongAsync()); } catch { return new DeserializeResult<ulong>(false, 0); } },
                async (r) => { try { return new DeserializeResult<TextData>(true, await r.ReadAsync<TextData>()); } catch { return new DeserializeResult<TextData>(false, null); } },
                async (r) => { try { return new DeserializeResult<TextData[]>(true, await r.ReadArrayAsync<TextData>()); } catch { return new DeserializeResult<TextData[]>(false, new TextData[0]); } });
        }

        public void Dispose()
        {
        }
    }

    public class PartitionStorageTests
        : IClassFixture<FSDBOptions>
    {
        private readonly FSDBOptions _options;
        public PartitionStorageTests(FSDBOptions options)
        {
            _options = options;
        }



        [Fact]
        public async Task FastFSDBTest()
        {
            var r = new Random(Environment.TickCount);
            var store = new Store<ulong, TextData, TextData[], Metadata>(_options.Options, _options.Serializers);

            // Arrange
            var numbers = new ulong[] { 86438 * 128, 83439 * 128, 131238 * 128 };

            var texts = new TextData[9]
            {
                new TextData { Title = "Title1", Text = "00" }, new TextData { Title = "Title2", Text = "01" }, new TextData { Title = "Title3", Text = "02" },
                new TextData { Title = "Title1", Text = "10" }, new TextData { Title = "Title2", Text = "11" }, new TextData { Title = "Title3", Text = "12" },
                new TextData { Title = "Title1", Text = "20" }, new TextData { Title = "Title2", Text = "21" }, new TextData { Title = "Title3", Text = "22" }
            };

            var testValues = new Dictionary<ulong, HashSet<string>> 
            {
                { numbers[0], new HashSet<string> { "0010", "01" } },
                { numbers[1], new HashSet<string> { "021222" } },
                { numbers[2], new HashSet<string> { "1121", "20" } }
            };

            Console.WriteLine("Small test start");

            // Act
            using (var storePart = store.CreateBuilder(new Metadata { Date = "20230720", Time = "15:00:00" }))
            {
                await storePart.Store(numbers[0], texts[0]);  // 1 - 00
                await storePart.Store(numbers[0], texts[3]);  // 1 - 10
                await storePart.Store(numbers[0], texts[1]);  // 2 - 01

                await storePart.Store(numbers[1], texts[2]);  // 3 - 02
                await storePart.Store(numbers[1], texts[5]);  // 3 - 12
                await storePart.Store(numbers[1], texts[8]);  // 3 - 22

                await storePart.Store(numbers[2], texts[4]);  // 2 - 11
                await storePart.Store(numbers[2], texts[6]);  // 1 - 20
                await storePart.Store(numbers[2], texts[7]);  // 2 - 21

                storePart.CompleteAdding();
                await storePart.Compress();
            }

            // Assert
            using (var readPart = store.CreateAccessor(new Metadata { Date = "20230720", Time = "15:00:00" }))
            {
                foreach (var number in numbers)
                {
                    var result = await readPart.Find(number);
                    if (result.Success)
                    {
                        foreach (var td in result.Value)
                        {
                            Assert.Contains(td.Text, testValues[number]);
                        }
                    }
                }
            }
        }
/*
        [Fact]
        public void IndexNoIndexFSDBTest()
        {

        }


        [Fact]
        public void StressFSDBTest()
        {

        }*/
    }
}
