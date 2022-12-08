﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.PartitionStorage.Partition;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class StorePartitionAccessor<TKey, TInput, TValue, TMeta>
        : BasePartition<TKey, TInput, TValue, TMeta>, IStorePartitionAccessor<TKey, TInput, TValue>
    {
        public StorePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            IStoreSerializer<TKey, TInput, TValue> serializer)
            : base(options, info, serializer)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
        }

        #region IStorePartitionAccessor
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
                            var k = Serializer.KeyDeserializer.Invoke(reader);
                            var v = Serializer.ValueDeserializer.Invoke(reader);
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
                                var k = Serializer.KeyDeserializer.Invoke(reader);
                                var v = Serializer.ValueDeserializer.Invoke(reader);
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
                            var k = Serializer.KeyDeserializer.Invoke(reader);
                            var v = Serializer.ValueDeserializer.Invoke(reader);
                            yield return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = k, Value = v, Status = SearchResult.Success };
                        }
                    }
                }
            }
        }
        public void RebuildIndex() => RebuildIndexes();
        public void RemoveAllExceptKey(TKey key, bool autoReindex = true)
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
                                    var k = Serializer.KeyDeserializer.Invoke(reader);
                                    var v = Serializer.ValueDeserializer.Invoke(reader);
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
                                var k = Serializer.KeyDeserializer.Invoke(reader);
                                var v = Serializer.ValueDeserializer.Invoke(reader);
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
                                    var k = Serializer.KeyDeserializer.Invoke(reader);
                                    Serializer.ValueDeserializer.Invoke(reader);
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
                                var k = Serializer.KeyDeserializer.Invoke(reader);
                                Serializer.ValueDeserializer.Invoke(reader);
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
                File.Move(tempFile, filePath, true);

                // Rebuild index if needs
                if (_options.Index.Enabled && autoReindex)
                {
                    RebuildFileIndex(filePath);
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
    }
}
