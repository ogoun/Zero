using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.Storages
{
    public class Partition<TKey>
    {
        public Partition(string name, Func<TKey, string> pathExtractor)
        {
            Name = name;
            PathExtractor = pathExtractor;
        }
        public Func<TKey, string> PathExtractor { get; }
        public string Name { get; }
    }
    /// <summary>
    /// Write data to partitional data storage
    /// </summary>
    public interface IPartitionFileStorage<TKey, TRecord>
    {
        Task WriteAsync(TKey key, IEnumerable<TRecord> records);
        Task<IEnumerable<TRecord>> CollectAsync(IEnumerable<TKey> keys);
        void Drop(TKey key);
    }

    public interface IPartitionDataConverter<TRecord>
    {
        IEnumerable<TRecord> ConvertFromStorage(Stream stream);
        void ConvertToStorage(Stream stream, IEnumerable<TRecord> data);
    }

    public class PartitionFileStorageOptions<TKey, TRecord>
    {
        public string RootFolder { get; set; }
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount / 2;
        public bool MergeFiles { get; set; } = false;
        public int MergeFrequencyInMinutes { get; set; } = 180;
        public bool UseCompression { get; set; } = false;
        public IPartitionDataConverter<TRecord> DataConverter { get; set; }
        public List<Partition<TKey>> Partitions { get; set; } = new List<Partition<TKey>>();
    }

    public class PartitionFileStorage<TKey, TRecord>
        : IPartitionFileStorage<TKey, TRecord>
    {

        private readonly PartitionFileStorageOptions<TKey, TRecord> _options;
        public PartitionFileStorage(PartitionFileStorageOptions<TKey, TRecord> options)
        {
            if (options.RootFolder == null)
                throw new ArgumentNullException(nameof(options.RootFolder));
            if (options.DataConverter == null)
                throw new ArgumentNullException(nameof(options.DataConverter));
            if (!Directory.Exists(options.RootFolder))
            {
                Directory.CreateDirectory(options.RootFolder);
            }
            _options = options;
            if (options.MergeFiles)
            {
                Sheduller.RemindEvery(TimeSpan.FromMinutes(_options.MergeFrequencyInMinutes), MergeDataFiles);
            }
        }

        public void Drop(TKey key)
        {
            var path = GetDataPath(key);
            FSUtils.RemoveFolder(path, 3, 500);
        }

        public async Task<IEnumerable<TRecord>> CollectAsync(IEnumerable<TKey> keys)
        {
            var pathes = keys.Safe().Select(k => GetDataPath(k));
            var files = pathes.Safe().SelectMany(p => Directory.GetFiles(p)).Where(n => n.StartsWith("__") == false);
            var set = new ConcurrentBag<TRecord>();
            if (files.Any())
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism };
                await Parallel.ForEachAsync(files, options, async (file, _) =>
                {
                    using (var stream = CreateReadStream(file))
                    {
                        foreach (var item in _options.DataConverter.ConvertFromStorage(stream))
                        {
                            set.Add(item);
                        }
                    }
                });
            }
            return set;
        }
        public async Task WriteAsync(TKey key, IEnumerable<TRecord> records)
        {
            using (var stream = CreateWriteStream(key))
            {
                _options.DataConverter.ConvertToStorage(stream, records);
                await stream.FlushAsync();
            }
        }

        #region Private members
        private void MergeDataFiles()
        {
            var folders = new Stack<string>();
            folders.Push(_options.RootFolder);
            while (folders.Count > 0)
            {
                var dir = folders.Pop();
                MergeFolder(dir);
                foreach (var subdir in Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly))
                {
                    folders.Push(subdir);
                }
            }
        }

        private void MergeFolder(string path)
        {
            var files = Directory.GetFiles(path);
            if (files != null && files.Length > 1)
            {
                
            }
        }

        private string GetDataFilePath(string path)
        {
            return Path.Combine(path, Guid.NewGuid().ToString());
        }
        private string GetDataPath(TKey key)
        {
            var path = _options.RootFolder;
            foreach (var partition in _options.Partitions)
            {
                var pathPart = partition.PathExtractor(key);
                pathPart = FSUtils.FileNameCorrection(pathPart);
                if (string.IsNullOrWhiteSpace(pathPart))
                {
                    throw new Exception($"Partition '{partition.Name}' not return name of part of path");
                }
                path = Path.Combine(path, pathPart);
            }
            return path;
        }
        private Stream CreateWriteStream(TKey key)
        {
            var path = GetDataPath(key);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var fullPath = GetDataFilePath(path);
            var stream = File.OpenWrite(fullPath);
            if (_options.UseCompression)
            {
                return new GZipStream(stream, CompressionMode.Compress, false);
            }
            return stream;
        }

        private Stream CreateReadStream(string path)
        {
            var stream = File.OpenRead(path);
            if (_options.UseCompression)
            {
                var ms = new MemoryStream();
                using (var compressed = new GZipStream(stream, CompressionMode.Decompress, false))
                { 
                    compressed.CopyTo(ms);
                }
                ms.Position = 0;
                return ms;
            }
            return stream;
        }
        #endregion
    }
}
