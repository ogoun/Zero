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
    public class StorePartitionBuilder<TKey, TInput, TValue, TMeta>
        : IStorePartitionBuilder<TKey, TInput, TValue>
    {
        private readonly ConcurrentDictionary<string, MemoryStreamWriter> _writeStreams
            = new ConcurrentDictionary<string, MemoryStreamWriter>();

        private readonly StoreOptions<TKey, TInput, TValue, TMeta> _options;
        private readonly string _catalog;
        private readonly TMeta _info;

        public string Catalog { get { return _catalog; } }
        public StorePartitionBuilder(StoreOptions<TKey, TInput, TValue, TMeta> options, TMeta info)
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

        public void Store(TKey key, TInput value)
        {
            var fileName = _options.GetFileName(key, _info);
            var stream = GetWriteStream(fileName);
            stream.SerializeCompatible(key);
            stream.SerializeCompatible(value);
        }
        public void CompleteAddingAndCompress()
        {
            CloseStreams();
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                Parallel.ForEach(files, file => CompressFile(file));
            }
        }
        public void RebuildIndex()
        {
            if (_options.Index.Enabled)
            {
                var indexFolder = Path.Combine(_catalog, "__indexes__");
                FSUtils.CleanAndTestFolder(indexFolder);
                var files = Directory.GetFiles(_catalog);
                if (files != null && files.Length > 0)
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

        internal void CloseStreams()
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
