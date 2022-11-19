using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StorePartitionAccessor<TKey, TInput, TValue, TMeta>
        : IStorePartitionAccessor<TKey, TInput, TValue>
    {
        private readonly StoreOptions<TKey, TInput, TValue, TMeta> _options;
        private readonly string _catalog;
        private readonly string _indexCatalog;
        private readonly TMeta _info;

        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly Func<MemoryStreamReader, TValue> _valueDeserializer;

        public string Catalog { get { return _catalog; } }
        public StorePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options, TMeta info)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _info = info;
            _options = options;
            _catalog = _options.GetCatalogPath(info);
            if (_options.Index.Enabled)
            {
                _indexCatalog = Path.Combine(_catalog, "__indexes__");
            }
            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
        }

        #region API methods
        public int CountDataFiles() => Directory.Exists(_catalog) ? (Directory.GetFiles(_catalog)?.Length ?? 0) : 0;
        public string GetCatalogPath() => _catalog;
        public void DropData() => FSUtils.CleanAndTestFolder(_catalog);
        public StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key)
        {
            var fileName = _options.GetFileName(key, _info);
            if (File.Exists(Path.Combine(_catalog, fileName)))
            {
                long startOffset = 0;
                if (_options.Index.Enabled)
                {
                    var index = new StorePartitionSparseIndex<TKey, TMeta>(_catalog, _info, _options.FilePartition, _options.KeyComparer);
                    var offset = index.GetOffset(key);
                    startOffset = offset.Offset;
                }
                if (TryGetReadStream(fileName, out var reader))
                {
                    using (reader)
                    {
                        if (startOffset > 0)
                        {
                            reader.Seek(startOffset, SeekOrigin.Begin);
                        }
                        while (reader.EOS == false)
                        {
                            var k = _keyDeserializer.Invoke(reader);
                            var v = _valueDeserializer.Invoke(reader);
                            var c = _options.KeyComparer(key, k);
                            if (c == 0) return new StorePartitionKeyValueSearchResult<TKey, TValue>
                            {
                                Key = key,
                                Value = v,
                                Status = SearchResult.Success
                            };
                            if (c == -1)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    return new StorePartitionKeyValueSearchResult<TKey, TValue>
                    {
                        Key = key,
                        Status = SearchResult.FileLocked,
                        Value = default
                    };
                }
            }
            return new StorePartitionKeyValueSearchResult<TKey, TValue>
            {
                Key = key,
                Status = SearchResult.NotFound,
                Value = default
            };
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.ToArray() });
            foreach (var group in results)
            {
                foreach (var r in Find(group.FileName, group.Keys))
                {
                    yield return r;
                }
            }
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Iterate()
        {
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (TryGetReadStream(file, out var reader))
                    {
                        using (reader)
                        {
                            while (reader.EOS == false)
                            {
                                var k = _keyDeserializer.Invoke(reader);
                                var v = _valueDeserializer.Invoke(reader);
                                yield return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = k, Value = v, Status = SearchResult.Success };
                            }
                        }
                    }
                }
            }
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> IterateKeyBacket(TKey key)
        {
            var fileName = _options.GetFileName(key, _info);
            if (File.Exists(Path.Combine(_catalog, fileName)))
            {
                if (TryGetReadStream(fileName, out var reader))
                {
                    using (reader)
                    {
                        while (reader.EOS == false)
                        {
                            var k = _keyDeserializer.Invoke(reader);
                            var v = _valueDeserializer.Invoke(reader);
                            yield return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = k, Value = v, Status = SearchResult.Success };
                        }
                    }
                }
            }
        }
        public void RebuildIndex()
        {
            if (_options.Index.Enabled)
            {
                FSUtils.CleanAndTestFolder(_indexCatalog);
                var files = Directory.GetFiles(_catalog);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        RebuildFileIndex(file);
                    }
                }
            }
        }
        public void RemoveAllExceptKey(TKey key, bool autoReindex = false)
        {
            RemoveAllExceptKeys(new[] { key }, autoReindex);
        }
        public void RemoveAllExceptKeys(IEnumerable<TKey> keys, bool autoReindex = true)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.OrderBy(k => k).ToArray() });
            foreach (var group in results)
            {
                RemoveKeyGroup(group.FileName, group.Keys, false, autoReindex);
            }
        }
        public void RemoveKey(TKey key, bool autoReindex = false)
        {
            RemoveKeys(new[] { key }, autoReindex);
        }
        public void RemoveKeys(IEnumerable<TKey> keys, bool autoReindex = true)
        {
            var results = keys.Distinct()
                .GroupBy(
                    k => _options.GetFileName(k, _info),
                    k => k, (key, g) => new { FileName = key, Keys = g.OrderBy(k => k).ToArray() });
            foreach (var group in results)
            {
                RemoveKeyGroup(group.FileName, group.Keys, true, autoReindex);
            }
        }
        #endregion

        #region Internal methods
        internal void DropFileIndex(string file)
        {
            if (_options.Index.Enabled)
            {
                var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
                if (File.Exists(index_file))
                {
                    File.Delete(index_file);
                }
            }
        }
        internal void RebuildFileIndex(string file)
        {
            if (_options.Index.Enabled)
            {
                if (false == Directory.Exists(_indexCatalog))
                {
                    Directory.CreateDirectory(_indexCatalog);
                }
                var dict = new Dictionary<TKey, long>();
                if (TryGetReadStream(file, out var reader))
                {
                    using (reader)
                    {
                        while (reader.EOS == false)
                        {
                            var pos = reader.Position;
                            var k = _keyDeserializer.Invoke(reader);
                            dict[k] = pos;
                            _valueDeserializer.Invoke(reader);
                        }
                    }
                }
                if (dict.Count > _options.Index.FileIndexCount * 8)
                {
                    var step = (int)Math.Round(((float)dict.Count / (float)_options.Index.FileIndexCount), MidpointRounding.ToZero);
                    var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
                    var d_arr = dict.OrderBy(p => p.Key).ToArray();
                    using (var writer = new MemoryStreamWriter(
                        new FileStream(index_file, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        for (int i = 0; i < _options.Index.FileIndexCount; i++)
                        {
                            var pair = d_arr[i * step];
                            writer.WriteCompatible(pair.Key);
                            writer.WriteLong(pair.Value);
                        }
                    }
                }
            }
        }
        #endregion

        #region Private methods
        private IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(string fileName,
            TKey[] keys)
        {
            if (File.Exists(Path.Combine(_catalog, fileName)))
            {
                if (_options.Index.Enabled)
                {
                    var index = new StorePartitionSparseIndex<TKey, TMeta>(_catalog, _info, _options.FilePartition, _options.KeyComparer);
                    var offsets = index.GetOffset(keys, true);
                    if (TryGetReadStream(fileName, out var reader))
                    {
                        using (reader)
                        {
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var searchKey = keys[i];
                                var off = offsets[i];
                                reader.Seek(off.Offset, SeekOrigin.Begin);
                                while (reader.EOS == false)
                                {
                                    var k = _keyDeserializer.Invoke(reader);
                                    var v = _valueDeserializer.Invoke(reader);
                                    var c = _options.KeyComparer(searchKey, k);
                                    if (c == 0)
                                    {
                                        yield return new StorePartitionKeyValueSearchResult<TKey, TValue>
                                        {
                                            Key = searchKey,
                                            Value = v,
                                            Status = SearchResult.Success
                                        };
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
                    if (TryGetReadStream(fileName, out var reader))
                    {
                        using (reader)
                        {
                            int index = 0;
                            var keys_arr = keys.OrderBy(k => k).ToArray();
                            while (reader.EOS == false && index < keys_arr.Length)
                            {
                                var k = _keyDeserializer.Invoke(reader);
                                var v = _valueDeserializer.Invoke(reader);
                                var c = _options.KeyComparer(keys_arr[index], k);
                                if (c == 0)
                                {
                                    yield return new StorePartitionKeyValueSearchResult<TKey, TValue>
                                    {
                                        Key = keys_arr[index],
                                        Value = v,
                                        Status = SearchResult.Success
                                    };
                                    index++;
                                }
                                else if (c == -1)
                                {
                                    do
                                    {
                                        index++;
                                        if (index < keys_arr.Length)
                                        {
                                            c = _options.KeyComparer(keys_arr[index], k);
                                        }
                                    } while (index < keys_arr.Length && c == -1);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RemoveKeyGroup(string fileName, TKey[] keys, bool inverseRemove, bool autoReindex)
        {
            var filePath = Path.Combine(_catalog, fileName);
            if (File.Exists(filePath))
            {
                // 1. Find ranges
                var ranges = new List<FilePositionRange>();
                if (_options.Index.Enabled && autoReindex)
                {
                    var index = new StorePartitionSparseIndex<TKey, TMeta>(_catalog, _info, _options.FilePartition, _options.KeyComparer);
                    var offsets = index.GetOffset(keys, true);
                    if (TryGetReadStream(fileName, out var reader))
                    {
                        using (reader)
                        {
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var searchKey = keys[i];
                                var off = offsets[i];
                                reader.Seek(off.Offset, SeekOrigin.Begin);
                                while (reader.EOS == false)
                                {
                                    var startPosition = reader.Position;
                                    var k = _keyDeserializer.Invoke(reader);
                                    _valueDeserializer.Invoke(reader);
                                    var endPosition = reader.Position;
                                    var c = _options.KeyComparer(searchKey, k);
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
                    if (TryGetReadStream(fileName, out var reader))
                    {
                        using (reader)
                        {
                            int index = 0;
                            var keys_arr = keys.OrderBy(k => k).ToArray();
                            while (reader.EOS == false && index < keys_arr.Length)
                            {
                                var startPosition = reader.Position;
                                var k = _keyDeserializer.Invoke(reader);
                                _valueDeserializer.Invoke(reader);
                                var endPosition = reader.Position;
                                var c = _options.KeyComparer(keys_arr[index], k);
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
                                            c = _options.KeyComparer(keys_arr[index], k);
                                        }
                                    } while (index < keys_arr.Length && c == -1);
                                }
                            }
                        }
                    }
                }

                // 2. Temporary file from ranges
                var tempPath = Path.GetTempPath();
                var tempFile = Path.Combine(tempPath, Path.GetTempFileName());

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
                File.Move(tempFile, filePath, true);

                // Rebuild index if needs
                if (_options.Index.Enabled && autoReindex)
                {
                    RebuildFileIndex(filePath);
                }
            }
        }

        private bool TryGetReadStream(string fileName, out MemoryStreamReader reader)
        {
            try
            {
                var filePath = Path.Combine(_catalog, fileName);
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
                reader = new MemoryStreamReader(stream);
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionAccessor.TryGetReadStream]");
            }
            reader = null;
            return false;
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

        public void Dispose()
        {
        }
    }
}
