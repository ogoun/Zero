using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.PartitionStorage.Partition;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Performs merging of new data with existing data in the partition
    /// </summary>
    internal sealed class StoreMergePartitionAccessor<TKey, TInput, TValue, TMeta>
        : IStorePartitionMergeBuilder<TKey, TInput, TValue>
    {
        private readonly Func<TValue, IEnumerable<TInput>> _decompress;
        /// <summary>
        /// Exists compressed catalog
        /// </summary>
        private readonly IStorePartitionAccessor<TKey, TInput, TValue> _accessor;
        private readonly PhisicalFileAccessorCachee _phisicalFileAccessor;
        private readonly string _temporaryFolder;
        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly Func<MemoryStreamReader, TValue> _valueDeserializer;

        public long TotalRecords
        {
            get { return _temporaryAccessor.TotalRecords; }
        }

        /// <summary>
        /// Write catalog
        /// </summary>
        private readonly IStorePartitionBuilder<TKey, TInput, TValue> _temporaryAccessor;

        public StoreMergePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            Func<TValue, IEnumerable<TInput>> decompress,
            IStoreSerializer<TKey, TInput, TValue> serializer,
            PhisicalFileAccessorCachee cachee)
        {
            if (decompress == null) throw new ArgumentNullException(nameof(decompress));
            _decompress = decompress;
            _accessor = new StorePartitionAccessor<TKey, TInput, TValue, TMeta>(options, info, serializer, cachee);
            _temporaryFolder = Path.Combine(_accessor.GetCatalogPath(), Guid.NewGuid().ToString());
            var tempOptions = options.Clone();
            tempOptions.RootFolder = _temporaryFolder;
            _temporaryAccessor = new StorePartitionBuilder<TKey, TInput, TValue, TMeta>(tempOptions, info, serializer, cachee);

            _phisicalFileAccessor = cachee;

            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
        }

        #region API methods
        /// <summary>
        /// Deletes only new entries. Existing entries remain unchanged.
        /// </summary>
        public void DropData() => _temporaryAccessor.DropData();
        public string GetCatalogPath() => _accessor.GetCatalogPath();
        public async Task Store(TKey key, TInput value) => await _temporaryAccessor.Store(key, value);
        public int CountDataFiles() => Math.Max(_accessor.CountDataFiles(),
                _temporaryAccessor.CountDataFiles());

        /// <summary>
        /// Performs compression/grouping of recorded data in a partition
        /// </summary>
        public async Task Compress()
        {
            var newFiles = Directory.GetFiles(_temporaryAccessor.GetCatalogPath());

            if (newFiles != null && newFiles.Length > 0)
            {
                var folder = _accessor.GetCatalogPath();
                var existsFiles = Directory.GetFiles(folder)
                    ?.ToDictionary(f => Path.GetFileName(f), f => f);

                foreach (var file in newFiles)
                {
                    var name = Path.GetFileName(file);
                    // if datafile by key exists
                    if (existsFiles.ContainsKey(name))
                    {
                        // append all records from existing file to new
                        foreach (var r in IterateReadKeyInputs(existsFiles[name]))
                        {
                            foreach (var i in r.Value)
                            {
                                await _temporaryAccessor.Store(r.Key, i);
                            }
                        }
                    }
                }

                _temporaryAccessor.CompleteAdding();

                // compress new file
                foreach (var file in newFiles)
                {
                    await (_temporaryAccessor as StorePartitionBuilder<TKey, TInput, TValue, TMeta>)
                            .CompressFile(file);
                }

                // replace old file by new
                foreach (var file in newFiles)
                {
                    _phisicalFileAccessor.DropDataReader(file);

                    // 1. Remove index file
                    (_accessor as StorePartitionAccessor<TKey, TInput, TValue, TMeta>)
                            .DropFileIndex(file);

                    // 2. Replace source
                    var name = Path.GetFileName(file);
                    var updateFilePath = Path.Combine(folder, name);

                    _phisicalFileAccessor.LockFile(updateFilePath);
                    try
                    {
                        if (File.Exists(updateFilePath))
                        {
                            File.Delete(updateFilePath);
                        }
                        File.Move(file, updateFilePath);
                    }
                    finally
                    {
                        _phisicalFileAccessor.UnlockFile(updateFilePath);
                    }

                    // 3. Rebuil index
                    await (_accessor as BasePartition<TKey, TInput, TValue, TMeta>).RebuildFileIndex(name);
                }
            }
            // remove temporary files
            _temporaryAccessor.DropData();
            Directory.Delete(_temporaryFolder, true);
        }
        #endregion

        #region Private methods
        private IEnumerable<SearchResult<TKey, IEnumerable<TInput>>>
            IterateReadKeyInputs(string filePath)
        {
            if (File.Exists(filePath))
            {
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
                using (var reader = new MemoryStreamReader(stream))
                {
                    while (reader.EOS == false)
                    {
                        var k = _keyDeserializer.Invoke(reader);
                        var v = _valueDeserializer.Invoke(reader);
                        var input = _decompress(v);
                        yield return new SearchResult<TKey, IEnumerable<TInput>>
                        {
                            Key = k,
                            Value = input,
                            Success = true
                        };
                    }
                }
            }
            else
            {
                Log.SystemError($"File not found '{filePath}'");
            }
        }
        #endregion

        public void Dispose()
        {
            _accessor.Dispose();
            _temporaryAccessor.Dispose();
        }
    }
}
