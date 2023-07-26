using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Memory;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.PartitionStorage.Partition;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class StorePartitionAccessor<TKey, TInput, TValue, TMeta>
        : BasePartition<TKey, TInput, TValue, TMeta>, IStorePartitionAccessor<TKey, TInput, TValue>
    {
        private readonly StorePartitionSparseIndex<TKey, TMeta> Indexes;
        public StorePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            IStoreSerializer<TKey, TInput, TValue> serializer,
            PhisicalFileAccessorCachee phisicalFileAccessor)
            : base(options, info, serializer, phisicalFileAccessor)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Index.Enabled)
            {
                Indexes = new StorePartitionSparseIndex<TKey, TMeta>(_catalog, _info, options.FilePartition, options.KeyComparer, options.Index.EnableIndexInMemoryCachee, phisicalFileAccessor);
            }
        }

        #region IStorePartitionAccessor       

        public async Task<SearchResult<TKey, TValue>> Find(TKey key)
        {
            IViewAccessor memoryAccessor;
            try
            {
                if (_options.Index.Enabled)
                {
                    var offset = Indexes.GetOffset(key);
                    memoryAccessor = offset.Length > 0 ? GetViewAccessor(key, offset.Offset, offset.Length) : GetViewAccessor(key, offset.Offset);
                }
                else
                {
                    memoryAccessor = GetViewAccessor(key, 0);
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[StorePartitionAccessor.Find] Fault get IViewAccessor by key {(key == null ? string.Empty : key.ToString())}");
                return new SearchResult<TKey, TValue>
                {
                    Key = key,
                    Success = false,
                    Value = default
                };
            }
            if (memoryAccessor != null)
            {
                using (var reader = new MemoryStreamReader(memoryAccessor))
                {
                    while (reader.EOS == false)
                    {
                        var kv = await Serializer.KeyDeserializer.Invoke(reader);
                        if (kv.Success == false) break;

                        var vv = await Serializer.ValueDeserializer.Invoke(reader);
                        if(vv.Success == false) break;

                        var c = _options.KeyComparer(key, kv.Value);
                        if (c == 0) return new SearchResult<TKey, TValue>
                        {
                            Key = key,
                            Value = vv.Value,
                            Success = true
                        };
                        if (c == -1)
                        {
                            break;
                        }
                    }
                }
            }
            return new SearchResult<TKey, TValue>
            {
                Key = key,
                Success = false,
                Value = default
            };
        }

        public async IAsyncEnumerable<KV<TKey, TValue>> Find(IEnumerable<TKey> keys)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.ToArray() });
            foreach (var group in results)
            {
                await foreach (var kv in Find(group.FileName, group.Keys))
                {
                    yield return kv;
                }
            }
        }

        public async IAsyncEnumerable<KV<TKey, TValue>> Iterate()
        {
            if (Directory.Exists(_catalog))
            {
                var files = Directory.GetFiles(_catalog);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        var accessor = PhisicalFileAccessorCachee.GetDataAccessor(file, 0);
                        if (accessor != null)
                        {
                            using (var reader = new MemoryStreamReader(accessor))
                            {
                                while (reader.EOS == false)
                                {
                                    var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                    if (kv.Success == false) break;

                                    var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                    if (vv.Success == false) break;

                                    yield return new KV<TKey, TValue>(kv.Value, vv.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        public async IAsyncEnumerable<TKey> IterateKeys()
        {
            if (Directory.Exists(_catalog))
            {
                var files = Directory.GetFiles(_catalog);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        var accessor = PhisicalFileAccessorCachee.GetDataAccessor(file, 0);
                        if (accessor != null)
                        {
                            using (var reader = new MemoryStreamReader(accessor))
                            {
                                while (reader.EOS == false)
                                {
                                    var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                    if (kv.Success == false) break;

                                    var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                    if (vv.Success == false) break;

                                    yield return kv.Value;
                                }
                            }
                        }
                    }
                }
            }
        }

        public async IAsyncEnumerable<KV<TKey, TValue>> IterateKeyBacket(TKey key)
        {
            var fileName = _options.GetFileName(key, _info);
            var filePath = Path.Combine(_catalog, fileName);
            if (File.Exists(filePath))
            {
                var accessor = PhisicalFileAccessorCachee.GetDataAccessor(filePath, 0);
                if (accessor != null)
                {
                    using (var reader = new MemoryStreamReader(accessor))
                    {
                        while (reader.EOS == false)
                        {
                            var kv = await Serializer.KeyDeserializer.Invoke(reader);
                            if (kv.Success == false) break;

                            var vv = await Serializer.ValueDeserializer.Invoke(reader);
                            if (vv.Success == false) break;

                            yield return new KV<TKey, TValue>(kv.Value, vv.Value);
                        }
                    }
                }
            }
        }
        public async Task RebuildIndex()
        {
            await RebuildIndexes();
            if (_options.Index.Enabled)
            {
                Indexes.ResetCachee();
            }
        }
        public async Task RemoveAllExceptKey(TKey key, bool autoReindex = true)
        {
            await RemoveAllExceptKeys(new[] { key }, autoReindex);
        }
        public async Task RemoveAllExceptKeys(IEnumerable<TKey> keys, bool autoReindex = true)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.OrderBy(k => k).ToArray() });
            foreach (var group in results)
            {
                await RemoveKeyGroup(group.FileName, group.Keys, false, autoReindex);
                if (_options.Index.Enabled)
                {
                    Indexes.RemoveCacheeItem(group.FileName);
                }
            }
        }
        public async Task RemoveKey(TKey key, bool autoReindex = false)
        {
            await RemoveKeys(new[] { key }, autoReindex);
        }
        public async Task RemoveKeys(IEnumerable<TKey> keys, bool autoReindex = true)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.OrderBy(k => k).ToArray() });
            foreach (var group in results)
            {
                await RemoveKeyGroup(group.FileName, group.Keys, true, autoReindex);
                if (_options.Index.Enabled)
                {
                    Indexes.RemoveCacheeItem(group.FileName);
                }
            }
        }
        #endregion


        #region Private methods
        private async IAsyncEnumerable<KV<TKey, TValue>> Find(string fileName, TKey[] keys)
        {
            var filePath = Path.Combine(_catalog, fileName);
            if (File.Exists(filePath))
            {
                if (_options.Index.Enabled)
                {
                    var offsets = Indexes.GetOffset(keys, true);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var searchKey = keys[i];
                        var offset = offsets[i];
                        IViewAccessor memoryAccessor;
                        if (offset.Length > 0)
                        {
                            memoryAccessor = GetViewAccessor(filePath, offset.Offset, offset.Length);
                        }
                        else
                        {
                            memoryAccessor = GetViewAccessor(filePath, offset.Offset);
                        }
                        if (memoryAccessor != null)
                        {
                            using (var reader = new MemoryStreamReader(memoryAccessor))
                            {
                                while (reader.EOS == false)
                                {
                                    var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                    if (kv.Success == false) break;

                                    var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                    if (vv.Success == false) break;

                                    var c = _options.KeyComparer(searchKey, kv.Value);
                                    if (c == 0)
                                    {
                                        yield return new KV<TKey, TValue>(kv.Value, vv.Value);
                                        break;
                                    }
                                    else if (c == -1)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var memoryAccessor = GetViewAccessor(filePath, 0);
                    if (memoryAccessor != null)
                    {
                        using (var reader = new MemoryStreamReader(memoryAccessor))
                        {
                            int index = 0;
                            var keys_arr = keys.OrderBy(k => k).ToArray();
                            while (reader.EOS == false && index < keys_arr.Length)
                            {
                                var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                if (kv.Success == false) break;

                                var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                if (vv.Success == false) break;

                                var c = _options.KeyComparer(keys_arr[index], kv.Value);
                                if (c == 0)
                                {
                                    yield return new KV<TKey, TValue>(kv.Value, vv.Value);
                                    index++;
                                }
                                else if (c == -1)
                                {
                                    do
                                    {
                                        index++;
                                        if (index < keys_arr.Length)
                                        {
                                            c = _options.KeyComparer(keys_arr[index], kv.Value);
                                        }
                                    } while (index < keys_arr.Length && c == -1);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task RemoveKeyGroup(string fileName, TKey[] keys, bool inverseRemove, bool autoReindex)
        {
            var filePath = Path.Combine(_catalog, fileName);
            if (File.Exists(filePath))
            {
                // 1. Find ranges
                var ranges = new List<FilePositionRange>();
                if (_options.Index.Enabled && autoReindex)
                {
                    var offsets = Indexes.GetOffset(keys, true);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var searchKey = keys[i];
                        var offset = offsets[i];
                        IViewAccessor memoryAccessor;
                        if (offset.Length > 0)
                        {
                            memoryAccessor = GetViewAccessor(filePath, offset.Offset, offset.Length);
                        }
                        else
                        {
                            memoryAccessor = GetViewAccessor(filePath, offset.Offset);
                        }
                        if (memoryAccessor != null)
                        {
                            using (var reader = new MemoryStreamReader(memoryAccessor))
                            {
                                while (reader.EOS == false)
                                {
                                    var startPosition = reader.Position;

                                    var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                    if (kv.Success == false)
                                    {
                                        Log.Error($"[StorePartitionAccessor.RemoveKeyGroup] Fault remove keys from file '{fileName}'. Incorrect file structure. Fault read key.");
                                        return;
                                    }

                                    var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                    if (vv.Success == false)
                                    {
                                        Log.Error($"[StorePartitionAccessor.RemoveKeyGroup] Fault remove keys from file '{fileName}'. Incorrect file structure. Fault read value.");
                                        return;
                                    }
                                    var endPosition = reader.Position;
                                    var c = _options.KeyComparer(searchKey, kv.Value);
                                    if (c == 0)
                                    {
                                        ranges.Add(new FilePositionRange { Start = startPosition, End = endPosition });
                                    }
                                    else if (c == -1)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var memoryAccessor = GetViewAccessor(filePath, 0);
                    if (memoryAccessor != null)
                    {
                        using (var reader = new MemoryStreamReader(memoryAccessor))
                        {
                            int index = 0;
                            var keys_arr = keys.OrderBy(k => k).ToArray();
                            while (reader.EOS == false && index < keys_arr.Length)
                            {
                                var startPosition = reader.Position;

                                var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                if (kv.Success == false)
                                {
                                    Log.Error($"[StorePartitionAccessor.RemoveKeyGroup] Fault remove keys from file '{fileName}'. Incorrect file structure. Fault read key.");
                                    return;
                                }

                                var vv = await Serializer.ValueDeserializer.Invoke(reader);
                                if (vv.Success == false)
                                {
                                    Log.Error($"[StorePartitionAccessor.RemoveKeyGroup] Fault remove keys from file '{fileName}'. Incorrect file structure. Fault read value.");
                                    return;
                                }

                                var endPosition = reader.Position;
                                var c = _options.KeyComparer(keys_arr[index], kv.Value);
                                if (c == 0)
                                {
                                    ranges.Add(new FilePositionRange { Start = startPosition, End = endPosition });
                                    index++;
                                }
                                else if (c == -1)
                                {
                                    do
                                    {
                                        index++;
                                        if (index < keys_arr.Length)
                                        {
                                            c = _options.KeyComparer(keys_arr[index], kv.Value);
                                        }
                                    } while (index < keys_arr.Length && c == -1);
                                }
                            }
                        }
                    }
                }

                // 2. Temporary file from ranges
                var tempFile = FSUtils.GetAppLocalTemporaryFile();

                using (var readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024))
                {
                    RangeCompression(ranges);
                    using (var writeStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 1024))
                    {
                        if (inverseRemove)
                        {
                            var inverted = RangeInversion(ranges, readStream.Length);
                            foreach (var range in inverted)
                            {
                                CopyRange(range, readStream, writeStream);
                            }
                        }
                        else
                        {
                            foreach (var range in ranges)
                            {
                                CopyRange(range, readStream, writeStream);
                            }
                        }
                        writeStream.Flush();
                    }
                }

                // 3. Replace from temporary to original
                PhisicalFileAccessorCachee.DropDataReader(filePath);
                File.Delete(filePath);
                File.Move(tempFile, filePath, true);

                // Rebuild index if needs
                if (_options.Index.Enabled && autoReindex)
                {
                    await RebuildFileIndex(filePath);
                }
            }
        }

        #endregion

        #region Static
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

        private static void CopyRange(FilePositionRange range, Stream source, Stream target)
        {
            source.Seek(range.Start, SeekOrigin.Begin);
            var size = range.End - range.Start;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, buffer.Length);
            target.Write(buffer, 0, buffer.Length);
        }
        #endregion

        public override void Release()
        {
            if (Directory.Exists(_catalog))
            {
                foreach (var file in Directory.GetFiles(_catalog))
                {
                    PhisicalFileAccessorCachee.DropDataReader(file);
                }
            }
            Indexes.ResetCachee();
        }
    }
}
