using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Provides write operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionBuilder<TKey, TInput, TValue>
        : IStorePartitionBase<TKey, TInput, TValue>
    {
        long TotalRecords
        {
            get;
        }
        IAsyncEnumerable<SearchResult<TKey, TInput>> Iterate();
        /// <summary>
        /// Writing a key-value pair
        /// </summary>
        Task Store(TKey key, TInput value);
        /// <summary>
        /// Called after all key-value pairs are written to the partition
        /// </summary>
        void CompleteAdding();
        /// <summary>
        /// Performs compression/grouping of recorded data in a partition
        /// </summary>
        void Compress();
        /// <summary>
        /// Rebuilds indexes for data in a partition
        /// </summary>
        Task RebuildIndex();
    }
}
