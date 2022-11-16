using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// For writing new values in exist partition
    /// 
    /// ORDER: Store -> CompleteAddingAndCompress -> RebuildIndex
    /// 
    /// </summary>
    public class StoreMergePartitionAccessor<TKey, TInput, TValue, TMeta>
        : IStorePartitionBuilder<TKey, TInput, TValue>
    {
        private readonly Func<TValue, IEnumerable<TInput>> _decompress;
        /// <summary>
        /// Exists compressed catalog
        /// </summary>
        private readonly IStorePartitionAccessor<TKey, TInput, TValue> _accessor;
        /// <summary>
        /// Write catalog
        /// </summary>
        private readonly IStorePartitionBuilder<TKey, TInput, TValue> _temporaryAccessor;
        public StoreMergePartitionAccessor(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info, Func<TValue, IEnumerable<TInput>> decompress)
        {
            if (decompress == null) throw new ArgumentNullException(nameof(decompress));
            _decompress = decompress;
            _accessor = new StorePartitionAccessor<TKey, TInput, TValue, TMeta>(options, info);
            var tempCatalog = Path.Combine(_accessor.GetCatalogPath(), Guid.NewGuid().ToString());
            var tempOptions = options.Clone();
            tempOptions.RootFolder = tempCatalog;
            _temporaryAccessor = new StorePartitionBuilder<TKey, TInput, TValue, TMeta>(tempOptions, info);
        }

        private IEnumerable<StorePartitionKeyValueSearchResult<TKey, IEnumerable<TInput>>>
            IterateReadKeyInputs(string filePath)
        {
            if (File.Exists(filePath))
            {
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
                using (var reader = new MemoryStreamReader(stream))
                {
                    while (reader.EOS == false)
                    {
                        var k = reader.ReadCompatible<TKey>();
                        var v = reader.ReadCompatible<TValue>();
                        var input = _decompress(v);
                        yield return
                            new StorePartitionKeyValueSearchResult<TKey, IEnumerable<TInput>>
                            {
                                Key = k,
                                Value = input,
                                Found = true
                            };
                    }
                }
            }
        }
        public void CompleteAddingAndCompress()
        {
            var newFiles = Directory.GetFiles(_temporaryAccessor.GetCatalogPath());

            if (newFiles != null && newFiles.Length > 1)
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
                                _temporaryAccessor.Store(r.Key, i);
                            }
                        }
                    }
                    // compress new file
                    (_temporaryAccessor as StorePartitionBuilder<TKey, TInput, TValue, TMeta>)
                            .CompressFile(file);
                    
                    // replace old file by new
                    File.Move(file, Path.Combine(folder, name), true);
                }
            }
            // remove temporary files
            _temporaryAccessor.DropData();
            Directory.Delete(_temporaryAccessor.GetCatalogPath(), true);
        }

        /// <summary>
        /// Deletes only new entries. Existing entries remain unchanged.
        /// </summary>
        public void DropData() => _temporaryAccessor.DropData();
        public string GetCatalogPath() => _accessor.GetCatalogPath();
        public void RebuildIndex() => _accessor.RebuildIndex();
        public void Store(TKey key, TInput value) => _temporaryAccessor.Store(key, value);
        public int CountDataFiles() => Math.Max(_accessor.CountDataFiles(),
                _temporaryAccessor.CountDataFiles());

        public void Dispose()
        {
            _accessor.Dispose();
            _temporaryAccessor.Dispose();
        }
    }
}
