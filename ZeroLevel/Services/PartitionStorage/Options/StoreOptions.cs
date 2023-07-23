using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Options
    /// </summary>
    /// <typeparam name="TKey">Record key</typeparam>
    /// <typeparam name="TInput">The value that is written in the stream</typeparam>
    /// <typeparam name="TValue">Value after compression of TInput values by duplicate keys (TInput list or similar)</typeparam>
    /// <typeparam name="TMeta">Meta information for partition search</typeparam>
    public class StoreOptions<TKey, TInput, TValue, TMeta>
    {
        private const string DEFAULT_FILE_NAME = "defaultGroup";
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
        /// <summary>
        /// Uses a thread-safe mechanism for writing to files during multi-threaded writes
        /// </summary>
        public bool ThreadSafeWriting { get; set; } = false;
        /// <summary>
        /// Period before memory mapped file was closed, after last access time
        /// </summary>
        public TimeSpan PhisicalFileAccessorExpirationPeriod { get; set; } = TimeSpan.FromMinutes(30);

        public IndexOptions Index { get; set; } = new IndexOptions
        {
            Enabled = false,
            StepValue = 64,
            StepType = IndexStepType.AbsoluteCount
        };

        internal string GetFileName(TKey key, TMeta info)
        {
            var name = FilePartition.FileNameExtractor(key, info);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = DEFAULT_FILE_NAME;
            }
            return name;
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

        public StoreOptions<TKey, TInput, TValue, TMeta> Clone()
        {
            var options = new StoreOptions<TKey, TInput, TValue, TMeta>
            {
                Index = new IndexOptions
                {
                    Enabled = this.Index.Enabled,
                    StepValue = 64,
                    StepType = IndexStepType.AbsoluteCount
                },
                FilePartition = this.FilePartition,
                KeyComparer = this.KeyComparer,
                MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
                MergeFunction = this.MergeFunction,
                Partitions = this.Partitions
                    .Select(p => new StoreCatalogPartition<TMeta>(p.Name, p.PathExtractor))
                    .ToList(),
                RootFolder = this.RootFolder,
                ThreadSafeWriting = this.ThreadSafeWriting
            };
            return options;
        }
    }
}
