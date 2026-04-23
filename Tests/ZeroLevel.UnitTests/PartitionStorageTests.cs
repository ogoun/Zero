using System;
using System.Collections.Generic;
using System.IO;
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
                storePart.Compress();
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
        // ===================================================================
        // Helpers — each test creates its own isolated store under the OS temp
        // folder so tests don't trample each other and run cross-platform.
        // ===================================================================

        private static (StoreOptions<ulong, TextData, TextData[], Metadata> opts, StoreSerializers<ulong, TextData, TextData[]> sers, string root) MakeIsolated(IndexOptions index = null!)
        {
            var root = Path.Combine(Path.GetTempPath(), "ZeroLevelPartitionTests", Guid.NewGuid().ToString("N"));
            FSUtils.CleanAndTestFolder(root);
            var opts = new StoreOptions<ulong, TextData, TextData[], Metadata>
            {
                Index = index ?? new IndexOptions { Enabled = true, StepType = IndexStepType.Step, StepValue = 32, EnableIndexInMemoryCachee = true },
                RootFolder = root,
                FilePartition = new StoreFilePartition<ulong, Metadata>("partition", (k, m) => (k % 128).ToString()),
                MergeFunction = list => list != null && list.Any()
                    ? list.GroupBy(i => i.Title).Select(p => new TextData { Title = p.Key, Text = string.Join(null, p.Select(t => t.Text)) }).ToArray()
                    : new TextData[0],
                Partitions = new List<StoreCatalogPartition<Metadata>> { new StoreCatalogPartition<Metadata>("Date", m => m.Date) },
                KeyComparer = (l, r) => l == r ? 0 : (l < r ? -1 : 1),
                ThreadSafeWriting = true,
                MaxDegreeOfParallelism = 8
            };
            var sers = new StoreSerializers<ulong, TextData, TextData[]>(
                async (w, n) => await w.WriteULongAsync(n),
                async (w, n) => await w.WriteAsync(n),
                async (w, n) => await w.WriteArrayAsync(n),
                async r => { try { return new DeserializeResult<ulong>(true, await r.ReadULongAsync()); } catch { return new DeserializeResult<ulong>(false, 0); } },
                async r => { try { return new DeserializeResult<TextData>(true, await r.ReadAsync<TextData>()); } catch { return new DeserializeResult<TextData>(false, null!); } },
                async r => { try { return new DeserializeResult<TextData[]>(true, await r.ReadArrayAsync<TextData>()); } catch { return new DeserializeResult<TextData[]>(false, new TextData[0]); } });
            return (opts, sers, root);
        }

        private static void Cleanup(string root) { try { FSUtils.RemoveFolder(root); } catch { } }

        private static Metadata Meta(string date = "20260424") => new Metadata { Date = date };

        // ===================================================================
        // 1. SingleRecordIndex_FindWorks — A6 regression
        // ===================================================================
        [Fact]
        public async Task SingleRecordIndex_FindWorks()
        {
            var (opts, sers, root) = MakeIsolated(new IndexOptions { Enabled = true, StepType = IndexStepType.Step, StepValue = 100, EnableIndexInMemoryCachee = true });
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(42UL, new TextData { Title = "T", Text = "single" });
                        b.CompleteAdding();
                        b.Compress();
                        await b.RebuildIndex();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        var r = await a.Find(42UL);
                        Assert.True(r.Success);
                        Assert.Single(r.Value);
                        Assert.Equal("single", r.Value[0].Text);
                        Assert.False((await a.Find(99UL)).Success);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 2. FindMultipleKeysUnsorted — B4 regression (binary-search shared position)
        // ===================================================================
        [Fact]
        public async Task FindMultipleKeysUnsorted()
        {
            var (opts, sers, root) = MakeIsolated(new IndexOptions { Enabled = true, StepType = IndexStepType.Step, StepValue = 2, EnableIndexInMemoryCachee = true });
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    // All keys land in the same partition file (k % 128 == 0).
                    var keys = new ulong[] { 128, 256, 384, 512, 640, 768, 896 };
                    using (var b = store.CreateBuilder(meta))
                    {
                        foreach (var k in keys)
                            await b.Store(k, new TextData { Title = "T" + k, Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                        await b.RebuildIndex();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        // shuffled order
                        var shuffled = new ulong[] { 640, 128, 896, 384, 512, 256, 768 };
                        var found = new Dictionary<ulong, string>();
                        await foreach (var kv in a.Find(shuffled))
                            found[kv.Key] = kv.Value[0].Text;
                        foreach (var k in keys)
                        {
                            Assert.True(found.ContainsKey(k), $"Key {k} not found via Find(IEnumerable)");
                            Assert.Equal("v" + k, found[k]);
                        }
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 3. WithoutIndex_BasicCRUD
        // ===================================================================
        [Fact]
        public async Task WithoutIndex_BasicCRUD()
        {
            var (opts, sers, root) = MakeIsolated(new IndexOptions { Enabled = false });
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(1UL, new TextData { Title = "A", Text = "1" });
                        await b.Store(2UL, new TextData { Title = "B", Text = "2" });
                        await b.Store(3UL, new TextData { Title = "C", Text = "3" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        Assert.Equal("1", (await a.Find(1UL)).Value[0].Text);
                        Assert.Equal("2", (await a.Find(2UL)).Value[0].Text);
                        Assert.Equal("3", (await a.Find(3UL)).Value[0].Text);
                        Assert.False((await a.Find(99UL)).Success);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 4. AbsoluteCountIndex_FindWorks
        // ===================================================================
        [Fact]
        public async Task AbsoluteCountIndex_FindWorks()
        {
            var (opts, sers, root) = MakeIsolated(new IndexOptions { Enabled = true, StepType = IndexStepType.AbsoluteCount, StepValue = 4, EnableIndexInMemoryCachee = true });
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        for (ulong k = 128; k <= 128 * 20; k += 128)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                        await b.RebuildIndex();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        for (ulong k = 128; k <= 128 * 20; k += 128)
                            Assert.Equal("v" + k, (await a.Find(k)).Value[0].Text);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 5. Iterate_ReturnsAllRecords
        // ===================================================================
        [Fact]
        public async Task Iterate_ReturnsAllRecords()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    var keys = new ulong[] { 128, 256, 384 };
                    using (var b = store.CreateBuilder(meta))
                    {
                        foreach (var k in keys)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        var seen = new HashSet<ulong>();
                        await foreach (var kv in a.Iterate())
                            seen.Add(kv.Key);
                        foreach (var k in keys) Assert.Contains(k, seen);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 6. IterateKeys_ReturnsAllKeys
        // ===================================================================
        [Fact]
        public async Task IterateKeys_ReturnsAllKeys()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    var keys = new ulong[] { 1, 2, 3, 4, 5 };
                    using (var b = store.CreateBuilder(meta))
                    {
                        foreach (var k in keys)
                            await b.Store(k, new TextData { Title = "T", Text = "v" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        var seen = new HashSet<ulong>();
                        await foreach (var k in a.IterateKeys())
                            seen.Add(k);
                        foreach (var k in keys) Assert.Contains(k, seen);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 7. IterateKeyBacket_ReturnsForOneKey
        // ===================================================================
        [Fact]
        public async Task IterateKeyBacket_ReturnsForOneKey()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        // 128 and 256 land in the same partition (both % 128 == 0)
                        await b.Store(128UL, new TextData { Title = "T", Text = "a" });
                        await b.Store(256UL, new TextData { Title = "T", Text = "b" });
                        // 1 lands in a different partition
                        await b.Store(1UL, new TextData { Title = "T", Text = "c" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        var keys = new HashSet<ulong>();
                        await foreach (var kv in a.IterateKeyBacket(128UL))
                            keys.Add(kv.Key);
                        Assert.Contains(128UL, keys);
                        Assert.Contains(256UL, keys);
                        Assert.DoesNotContain(1UL, keys); // different partition file
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 8. RemoveKey_ThenFindReturnsFalse
        // ===================================================================
        [Fact]
        public async Task RemoveKey_ThenFindReturnsFalse()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(128UL, new TextData { Title = "T", Text = "a" });
                        await b.Store(256UL, new TextData { Title = "T", Text = "b" });
                        await b.Store(384UL, new TextData { Title = "T", Text = "c" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        await a.RemoveKey(256UL, autoReindex: true);
                        Assert.True((await a.Find(128UL)).Success);
                        Assert.False((await a.Find(256UL)).Success);
                        Assert.True((await a.Find(384UL)).Success);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 9. RemoveKeys_ThenFindReturnsFalse
        // ===================================================================
        [Fact]
        public async Task RemoveKeys_ThenFindReturnsFalse()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        for (ulong k = 128; k <= 128 * 6; k += 128)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        await a.RemoveKeys(new ulong[] { 256UL, 512UL, 768UL }, autoReindex: true);
                        Assert.True((await a.Find(128UL)).Success);
                        Assert.False((await a.Find(256UL)).Success);
                        Assert.True((await a.Find(384UL)).Success);
                        Assert.False((await a.Find(512UL)).Success);
                        Assert.True((await a.Find(640UL)).Success);
                        Assert.False((await a.Find(768UL)).Success);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 10. RemoveAllExceptKey_KeptAndRemoved — also exercises B3 (CopyRange)
        // ===================================================================
        [Fact]
        public async Task RemoveAllExceptKey_KeptAndRemoved()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        for (ulong k = 128; k <= 128 * 8; k += 128)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        await a.RemoveAllExceptKey(384UL, autoReindex: true);
                        Assert.True((await a.Find(384UL)).Success);
                        for (ulong k = 128; k <= 128 * 8; k += 128)
                            if (k != 384UL)
                                Assert.False((await a.Find(k)).Success, $"Key {k} should have been removed");
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 11. Exists_ReturnsCorrectResult
        // ===================================================================
        [Fact]
        public async Task Exists_ReturnsCorrectResult()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(42UL, new TextData { Title = "T", Text = "x" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    Assert.True(await store.Exists(meta, 42UL));
                    Assert.False(await store.Exists(meta, 99UL));
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 12. RemovePartition_DeletesFolder
        // ===================================================================
        [Fact]
        public async Task RemovePartition_DeletesFolder()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                string partitionPath;
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta("20260424");
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(1UL, new TextData { Title = "T", Text = "x" });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                        partitionPath = a.GetCatalogPath();
                    Assert.True(Directory.Exists(partitionPath));
                    store.RemovePartition(meta);
                    Assert.False(Directory.Exists(partitionPath));
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 13. MergeAccessor_AddsToExisting — also exercises A8 (Clone settings)
        // ===================================================================
        [Fact]
        public async Task MergeAccessor_AddsToExisting()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    // First write
                    using (var b = store.CreateBuilder(meta))
                    {
                        await b.Store(128UL, new TextData { Title = "T", Text = "v1" });
                        b.CompleteAdding();
                        b.Compress();
                        await b.RebuildIndex();
                    }
                    // Merge: add a second value for the same key
                    using (var m = store.CreateMergeAccessor(meta, v => v))
                    {
                        await m.Store(128UL, new TextData { Title = "T", Text = "v2" });
                        await m.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        var r = await a.Find(128UL);
                        Assert.True(r.Success);
                        // Both v1 and v2 should be present (merged via MergeFunction)
                        var combinedText = r.Value[0].Text;
                        Assert.Contains("v1", combinedText);
                        Assert.Contains("v2", combinedText);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 14. RebuildIndex_AfterDataChange
        // ===================================================================
        [Fact]
        public async Task RebuildIndex_AfterDataChange()
        {
            var (opts, sers, root) = MakeIsolated(new IndexOptions { Enabled = true, StepType = IndexStepType.Step, StepValue = 2, EnableIndexInMemoryCachee = true });
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    using (var b = store.CreateBuilder(meta))
                    {
                        for (ulong k = 128; k <= 128 * 6; k += 128)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                        await b.RebuildIndex();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        await a.RemoveKey(384UL, autoReindex: false);
                        await a.RebuildIndex(); // explicit rebuild
                        for (ulong k = 128; k <= 128 * 6; k += 128)
                        {
                            var r = await a.Find(k);
                            if (k == 384UL) Assert.False(r.Success);
                            else Assert.True(r.Success, $"Find({k}) failed after RebuildIndex");
                        }
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 15. EmptyPartition_FindReturnsFalse
        // ===================================================================
        [Fact]
        public async Task EmptyPartition_FindReturnsFalse()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    // Create the partition folder by issuing an accessor on a builder that wrote nothing
                    using (var b = store.CreateBuilder(meta))
                    {
                        b.CompleteAdding();
                        b.Compress();
                    }
                    using (var a = store.CreateAccessor(meta))
                    {
                        Assert.False((await a.Find(42UL)).Success);
                    }
                }
            }
            finally { Cleanup(root); }
        }

        // ===================================================================
        // 16. Bypass_ReturnsAllRecords
        // ===================================================================
        [Fact]
        public async Task Bypass_ReturnsAllRecords()
        {
            var (opts, sers, root) = MakeIsolated();
            try
            {
                using (var store = new Store<ulong, TextData, TextData[], Metadata>(opts, sers))
                {
                    var meta = Meta();
                    var keys = new ulong[] { 1, 2, 3, 128, 256 };
                    using (var b = store.CreateBuilder(meta))
                    {
                        foreach (var k in keys)
                            await b.Store(k, new TextData { Title = "T", Text = "v" + k });
                        b.CompleteAdding();
                        b.Compress();
                    }
                    var seen = new HashSet<ulong>();
                    await foreach (var kv in store.Bypass(meta))
                        seen.Add(kv.Key);
                    foreach (var k in keys) Assert.Contains(k, seen);
                }
            }
            finally { Cleanup(root); }
        }
    }
}
