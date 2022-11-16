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
        private readonly TMeta _info;

        public string Catalog { get { return _catalog; } }
        public StorePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options, TMeta info)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _info = info;
            _options = options;
            _catalog = _options.GetCatalogPath(info);
        }

        public int CountDataFiles() => Directory.GetFiles(_catalog)?.Length ?? 0;
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
                using (var reader = GetReadStream(fileName))
                {
                    if (startOffset > 0)
                    {
                        reader.Stream.Seek(startOffset, SeekOrigin.Begin);
                    }
                    while (reader.EOS == false)
                    {
                        var k = reader.ReadCompatible<TKey>();
                        var v = reader.ReadCompatible<TValue>();
                        var c = _options.KeyComparer(key, k);
                        if (c == 0) return new StorePartitionKeyValueSearchResult<TKey, TValue>
                        {
                            Key = key,
                            Value = v,
                            Found = true
                        };
                        if (c == -1)
                        {
                            break;
                        }
                    }
                }
            }
            return new StorePartitionKeyValueSearchResult<TKey, TValue>
            {
                Key = key,
                Found = false,
                Value = default
            };
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys)
        {
            var results = keys
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
            if (files != null && files.Length > 1)
            {
                foreach (var file in files)
                {
                    using (var reader = GetReadStream(Path.GetFileName(file)))
                    {
                        while (reader.EOS == false)
                        {
                            var k = reader.ReadCompatible<TKey>();
                            var v = reader.ReadCompatible<TValue>();
                            yield return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = k, Value = v, Found = true };
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
                using (var reader = GetReadStream(fileName))
                {
                    while (reader.EOS == false)
                    {
                        var k = reader.ReadCompatible<TKey>();
                        var v = reader.ReadCompatible<TValue>();
                        yield return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = k, Value = v, Found = true };
                    }
                }
            }
        }
        public void RebuildIndex()
        {
            if (_options.Index.Enabled)
            {
                var indexFolder = Path.Combine(_catalog, "__indexes__");
                FSUtils.CleanAndTestFolder(indexFolder);
                var files = Directory.GetFiles(_catalog);
                if (files != null && files.Length > 1)
                {
                    var dict = new Dictionary<TKey, long>();
                    foreach (var file in files)
                    {
                        dict.Clear();
                        using (var reader = GetReadStream(Path.GetFileName(file)))
                        {
                            while (reader.EOS == false)
                            {
                                var pos = reader.Stream.Position;
                                var k = reader.ReadCompatible<TKey>();
                                dict[k] = pos;
                                reader.ReadCompatible<TValue>();
                            }
                        }
                        if (dict.Count > _options.Index.FileIndexCount * 8)
                        {
                            var step = (int)Math.Round(((float)dict.Count / (float)_options.Index.FileIndexCount), MidpointRounding.ToZero);
                            var index_file = Path.Combine(indexFolder, Path.GetFileName(file));
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
            }
        }

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
                    using (var reader = GetReadStream(fileName))
                    {
                        for (int i = 0; i < keys.Length; i++)
                        {
                            var searchKey = keys[i];
                            var off = offsets[i];
                            reader.Stream.Seek(off.Offset, SeekOrigin.Begin);
                            while (reader.EOS == false)
                            {
                                var k = reader.ReadCompatible<TKey>();
                                var v = reader.ReadCompatible<TValue>();
                                var c = _options.KeyComparer(searchKey, k);
                                if (c == 0)
                                {
                                    yield return new StorePartitionKeyValueSearchResult<TKey, TValue>
                                    {
                                        Key = searchKey,
                                        Value = v,
                                        Found = true
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
                else
                {
                    using (var reader = GetReadStream(fileName))
                    {
                        int index = 0;
                        var keys_arr = keys.OrderBy(k => k).ToArray();
                        while (reader.EOS == false && index < keys_arr.Length)
                        {
                            var k = reader.ReadCompatible<TKey>();
                            var v = reader.ReadCompatible<TValue>();
                            var c = _options.KeyComparer(keys_arr[index], k);
                            if (c == 0)
                            {
                                yield return new StorePartitionKeyValueSearchResult<TKey, TValue>
                                {
                                    Key = keys_arr[index],
                                    Value = v,
                                    Found = true
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
      
        private MemoryStreamReader GetReadStream(string fileName)
        {
            var filePath = Path.Combine(_catalog, fileName);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
            return new MemoryStreamReader(stream);
        }
        #endregion
        public void Dispose()
        {
        }
    }
}
