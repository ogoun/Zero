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

        public StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key)
        {
            var fileName = _options.GetFileName(key, _info);
            using (var reader = GetReadStream(fileName))
            {
                while (reader.EOS == false)
                {
                    var k = reader.ReadCompatible<TKey>();
                    var v = reader.ReadCompatible<TValue>();
                    var c = _options.KeyComparer(key, k);
                    if (c == 0) return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = key, Value = v, Found = true };
                    if (c == -1) break;
                }
            }
            return new StorePartitionKeyValueSearchResult<TKey, TValue> { Key = key, Found = false, Value = default };
        }

        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys)
        {
            foreach (var key in keys)
            {
                yield return Find(key);
            }
        }

        public void CompleteStoreAndRebuild()
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

        private void CompressFile(string file)
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

        public void Store(TKey key, TInput value)
        {
            var fileName = _options.GetFileName(key, _info);
            var stream = GetWriteStream(fileName);
            stream.SerializeCompatible(key);
            stream.SerializeCompatible(value);
        }

        private readonly ConcurrentDictionary<string, MemoryStreamWriter> _writeStreams = new ConcurrentDictionary<string, MemoryStreamWriter>();
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

        public void Dispose()
        {
        }

        public int CountDataFiles()
        {
            var files = Directory.GetFiles(_catalog);
            return files?.Length ?? 0;
        }

        public void DropData()
        {
            FSUtils.CleanAndTestFolder(_catalog);
        }
    }
}
