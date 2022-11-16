using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StorePartitionAccessor<TKey, TInput, TValue, TMeta>
        : IStorePartitionAccessor<TKey, TInput, TValue>
    {
        private readonly ConcurrentDictionary<string, MemoryStreamWriter> _writeStreams
            = new ConcurrentDictionary<string, MemoryStreamWriter>();
        private readonly IStoreOptions<TKey, TInput, TValue, TMeta> _options;
        private readonly string _catalog;
        private readonly TMeta _info;

        public string Catalog { get { return _catalog; } }
        public StorePartitionAccessor(IStoreOptions<TKey, TInput, TValue, TMeta> options, TMeta info)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _info = info;
            _options = options;
            _catalog = _options.GetCatalogPath(info);
            if (Directory.Exists(_catalog) == false)
            {
                Directory.CreateDirectory(_catalog);
            }
        }

        public int CountDataFiles() => Directory.GetFiles(_catalog)?.Length ?? 0;
        public string GetCatalogPath() => _catalog;
        public void DropData() => FSUtils.CleanAndTestFolder(_catalog);

        #region API !only after data compression!
        public StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key)
        {
            var fileName = _options.GetFileName(key, _info);
            if (File.Exists(Path.Combine(_catalog, fileName)))
            {
                long startOffset = 0;
                if (_options.Index.Enabled)
                {
                    var index = new StorePartitionIndex<TKey, TMeta>(_catalog, _info, _options.FilePartition, _options.KeyComparer);
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
        #endregion


        #region API !only before data compression!
        public void Store(TKey key, TInput value)
        {
            var fileName = _options.GetFileName(key, _info);
            var stream = GetWriteStream(fileName);
            stream.SerializeCompatible(key);
            stream.SerializeCompatible(value);
        }
        public void CompleteAddingAndCompress()
        {
            // Close all write streams
            foreach (var s in _writeStreams)
            {
                try
                {
                    s.Value.Dispose();
                }
                catch { }
            }
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 1)
            {
                Parallel.ForEach(files, file => CompressFile(file));
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
                    var index = new StorePartitionIndex<TKey, TMeta>(_catalog, _info, _options.FilePartition, _options.KeyComparer);
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
        internal void CompressFile(string file)
        {
            var dict = new Dictionary<TKey, HashSet<TInput>>();
            using (var reader = new MemoryStreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 4096 * 1024)))
            {
                while (reader.EOS == false)
                {
                    TKey k = reader.ReadCompatible<TKey>();
                    TInput v = reader.ReadCompatible<TInput>();
                    if (false == dict.ContainsKey(k))
                    {
                        dict[k] = new HashSet<TInput>();
                    }
                    dict[k].Add(v);
                }
            }
            var tempPath = Path.GetTempPath();
            var tempFile = Path.Combine(tempPath, Path.GetTempFileName());
            using (var writer = new MemoryStreamWriter(new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 1024)))
            {
                // sort for search acceleration
                foreach (var pair in dict.OrderBy(p => p.Key))
                {
                    var v = _options.MergeFunction(pair.Value);
                    writer.SerializeCompatible(pair.Key);
                    writer.SerializeCompatible(v);
                }
            }
            File.Delete(file);
            File.Move(tempFile, file, true);
        }        
        private MemoryStreamWriter GetWriteStream(string fileName)
        {
            return _writeStreams.GetOrAdd(fileName, k =>
            {
                var filePath = Path.Combine(_catalog, k);
                var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096 * 1024);
                return new MemoryStreamWriter(stream);
            });
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
