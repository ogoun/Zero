using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.PartitionStorage
{
    public class IndexOptions
    {
        public bool Enabled { get; set; }
        public int FileIndexCount { get; set; } = 64;
    }

    /// <summary>
    /// Options
    /// </summary>
    /// <typeparam name="TKey">Record key</typeparam>
    /// <typeparam name="TInput">The value that is written in the stream</typeparam>
    /// <typeparam name="TValue">Value after compression of TInput values by duplicate keys (TInput list or similar)</typeparam>
    /// <typeparam name="TMeta">Meta information for partition search</typeparam>
    public class IStoreOptions<TKey, TInput, TValue, TMeta>
    {
        /// <summary>
        /// Method for key comparison
        /// </summary>
        public Func<TKey, TKey, int> KeyComparer { get; set; }

        /// <summary>
        /// Storage root directory
        /// </summary>
        public string RootFolder { get; set; }
        /// <summary>
        /// Maximum degree of parallelis
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 64;
        /// <summary>
        /// Function for translating a list of TInput into one TValue
        /// </summary>
        public Func<IEnumerable<TInput>, TValue> MergeFunction { get; set; }
        /// <summary>
        /// List of partitions for accessing the catalog
        /// </summary>
        public List<StoreCatalogPartition<TMeta>> Partitions { get; set; } = new List<StoreCatalogPartition<TMeta>>();
        /// <summary>
        /// File Partition
        /// </summary>
        public StoreFilePartition<TKey, TMeta> FilePartition { get; set; }

        public IndexOptions Index { get; set; } = new IndexOptions { Enabled = false, FileIndexCount = 64 };

        internal string GetFileName(TKey key, TMeta info)
        {
            return FilePartition.PathExtractor(key, info);
        }
        internal string GetCatalogPath(TMeta info)
        {
            var path = RootFolder;
            foreach (var partition in Partitions)
            {
                var pathPart = partition.PathExtractor(info);
                pathPart = FSUtils.FileNameCorrection(pathPart);
                if (string.IsNullOrWhiteSpace(pathPart))
                {
                    throw new Exception($"Partition '{partition.Name}' not return name of part of path");
                }
                path = Path.Combine(path, pathPart);
            }
            return path;
        }

        public IStoreOptions<TKey, TInput, TValue, TMeta> Clone()
        {
            var options = new IStoreOptions<TKey, TInput, TValue, TMeta>
            {
                Index = new IndexOptions
                {
                    Enabled = this.Index.Enabled,
                    FileIndexCount = this.Index.FileIndexCount
                },
                FilePartition = this.FilePartition,
                KeyComparer = this.KeyComparer,
                MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
                MergeFunction = this.MergeFunction,
                Partitions = this.Partitions
                    .Select(p => new StoreCatalogPartition<TMeta>(p.Name, p.PathExtractor))
                    .ToList(),
                RootFolder = this.RootFolder
            };
            return options;
        }
    }
}
