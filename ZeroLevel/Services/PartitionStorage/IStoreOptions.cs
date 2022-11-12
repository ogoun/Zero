using System;
using System.Collections.Generic;
using System.IO;
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
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount / 2;
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
    }
}
