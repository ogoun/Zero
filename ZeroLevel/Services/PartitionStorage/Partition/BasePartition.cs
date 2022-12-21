using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage.Partition
{
    /// <summary>
    /// General operations with a partition
    /// </summary>
    internal abstract class BasePartition<TKey, TInput, TValue, TMeta>
        : IStorePartitionBase<TKey, TInput, TValue>
    {
        public string Catalog { get { return _catalog; } }

        protected readonly TMeta _info;
        protected readonly string _catalog;
        protected IStoreSerializer<TKey, TInput, TValue> Serializer { get; }
        protected readonly StoreOptions<TKey, TInput, TValue, TMeta> _options;

        private readonly IndexBuilder<TKey, TValue> _indexBuilder;
        private readonly Dictionary<string, MemoryStreamWriter> _writeStreams = new Dictionary<string, MemoryStreamWriter>();

        internal BasePartition(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            IStoreSerializer<TKey, TInput, TValue> serializer)
        {
            _options = options;
            _info = info;
            _catalog = _options.GetCatalogPath(info);
            if (Directory.Exists(_catalog) == false)
            {
                Directory.CreateDirectory(_catalog);
            }
            _indexBuilder = _options.Index.Enabled ? new IndexBuilder<TKey, TValue>(_options.Index.StepType, _options.Index.StepValue, _catalog) : null;
            Serializer = serializer;
        }

        #region IStorePartitionBase
        public int CountDataFiles() => Directory.Exists(_catalog) ? (Directory.GetFiles(_catalog)?.Length ?? 0) : 0;
        public string GetCatalogPath() => _catalog;
        public void DropData() => FSUtils.CleanAndTestFolder(_catalog);
        public void Dispose()
        {
            CloseWriteStreams();
        }
        #endregion

        /// <summary>
        /// Rebuild indexes for all files
        /// </summary>
        protected void RebuildIndexes()
        {
            if (_options.Index.Enabled)
            {
                _indexBuilder.RebuildIndex();
            }
        }
        /// <summary>
        /// Rebuild index for the specified file
        /// </summary>
        internal void RebuildFileIndex(string file)
        {
            if (_options.Index.Enabled)
            {
                _indexBuilder.RebuildFileIndex(file);
            }
        }
        /// <summary>
        /// Delete the index for the specified file
        /// </summary>
        internal void DropFileIndex(string file)
        {
            if (_options.Index.Enabled)
            {
                _indexBuilder.DropFileIndex(file);
            }
        }
        /// <summary>
        /// Close all streams for writing
        /// </summary>
        protected void CloseWriteStreams()
        {
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
        /// <summary>
        /// Attempting to open a file for writing
        /// </summary>
        protected bool TryGetWriteStream(string fileName, out MemoryStreamWriter writer)
        {
            try
            {
                bool taken = false;
                Monitor.Enter(_writeStreams, ref taken);
                try
                {
                    if (_writeStreams.TryGetValue(fileName, out var w))
                    {
                        writer = w;
                        return true;
                    }
                    else
                    {
                        var filePath = Path.Combine(_catalog, fileName);
                        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096 * 1024);
                        var new_w = new MemoryStreamWriter(stream);
                        _writeStreams[fileName] = new_w;
                        writer = new_w;
                        return true;
                    }
                }
                finally
                {
                    if (taken)
                    {
                        Monitor.Exit(_writeStreams);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionBuilder.TryGetWriteStream]");
            }
            writer = null;
            return false;
        }
        /// <summary>
        /// Attempting to open a file for reading
        /// </summary>
        protected bool TryGetReadStream(string fileName, out MemoryStreamReader reader)
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
    }
}
