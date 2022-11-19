using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly Action<MemoryStreamWriter, TKey> _keySerializer;
        private readonly Action<MemoryStreamWriter, TInput> _inputSerializer;

        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly Func<MemoryStreamReader, TInput> _inputDeserializer;
        private readonly Func<MemoryStreamReader, TValue> _valueDeserializer;
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

            _keySerializer = MessageSerializer.GetSerializer<TKey>();
            _inputSerializer = MessageSerializer.GetSerializer<TInput>();

            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _inputDeserializer = MessageSerializer.GetDeserializer<TInput>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
        }

        #region API methods
        public int CountDataFiles() => Directory.Exists(_catalog) ? (Directory.GetFiles(_catalog)?.Length ?? 0) : 0;
        public string GetCatalogPath() => _catalog;
        public void DropData() => FSUtils.CleanAndTestFolder(_catalog);
        public void Store(TKey key, TInput value)
        {
            var fileName = _options.GetFileName(key, _info);
            if (TryGetWriteStream(fileName, out var stream))
            {
                _keySerializer.Invoke(stream, key);
                Thread.MemoryBarrier();
                _inputSerializer.Invoke(stream, value);
            }
        }
        public void CompleteAdding()
        {
            // Close all write streams
            foreach (var s in _writeStreams)
            {
                try
                {
                    s.Value.Stream.Flush();
                    s.Value.Dispose();
                }
                catch { }
            }
            _writeStreams.Clear();
        }
        public void Compress()
        {
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                Parallel.ForEach(files, file => CompressFile(file));
            }
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TInput>> Iterate()
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
                                var key = _keyDeserializer.Invoke(reader);
                                var val = _inputDeserializer.Invoke(reader);
                                yield return new StorePartitionKeyValueSearchResult<TKey, TInput> { Key = key, Value = val, Status = SearchResult.Success };
                            }
                        }
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
                if (files != null && files.Length > 0)
                {
                    var dict = new Dictionary<TKey, long>();
                    foreach (var file in files)
                    {
                        if (TryGetReadStream(file, out var reader))
                        {
                            using (reader)
                            {
                                while (reader.EOS == false)
                                {
                                    var pos = reader.Stream.Position;
                                    var key = _keyDeserializer.Invoke(reader);
                                    dict[key] = pos;
                                    if (reader.EOS) break;
                                    _valueDeserializer.Invoke(reader);
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
                        dict.Clear();
                    }
                }
            }
        }
        #endregion

        #region Private methods
        internal void CompressFile(string file)
        {
            var dict = new Dictionary<TKey, HashSet<TInput>>();
            using (var reader = new MemoryStreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 4096 * 1024)))
            {
                while (reader.EOS == false)
                {
                    var key = _keyDeserializer.Invoke(reader);
                    if (false == dict.ContainsKey(key))
                    {
                        dict[key] = new HashSet<TInput>();
                    }
                    if (reader.EOS)
                    {
                        break;
                    }
                    var input = _inputDeserializer.Invoke(reader);
                    dict[key].Add(input);
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
        private bool TryGetWriteStream(string fileName, out MemoryStreamWriter writer)
        {
            try
            {
                writer = _writeStreams.GetOrAdd(fileName, k =>
                {
                    var filePath = Path.Combine(_catalog, k);
                    var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096 * 1024);
                    return new MemoryStreamWriter(stream);
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionBuilder.TryGetWriteStream]");
            }
            writer = null;
            return false;
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
                Log.SystemError(ex, "[StorePartitionBuilder.TryGetReadStream]");
            }
            reader = null;
            return false;
        }
        #endregion

        public void Dispose()
        {
        }
    }
}
