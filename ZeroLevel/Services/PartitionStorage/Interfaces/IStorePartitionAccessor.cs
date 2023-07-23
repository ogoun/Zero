using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Provides read/reindex operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionAccessor<TKey, TInput, TValue>
        : IStorePartitionBase<TKey, TInput, TValue>
    {
        /// <summary>
        /// Rebuilds indexes for data in a partition
        /// </summary>
        Task RebuildIndex();
        /// <summary>
        /// Search in a partition for a specified key
        /// </summary>
        Task<SearchResult<TKey, TValue>> Find(TKey key);
        /// <summary>
        /// Search in a partition for a specified keys
        /// </summary>
        IAsyncEnumerable<KV<TKey, TValue>> Find(IEnumerable<TKey> keys);

        /// <summary>
        /// Iterating over all recorded data
        /// </summary>
        IAsyncEnumerable<KV<TKey, TValue>> Iterate();
        /// <summary>
        /// Iterating over all recorded data of the file with the specified key
        /// </summary>
        Task IterateKeyBacket(TKey key, Action<TKey, TValue> kvHandler);
        /// <summary>
        /// Deleting the specified key and associated data
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = false)</param>
        Task RemoveKey(TKey key, bool autoReindex = false);
        /// <summary>
        /// Deleting the specified keys and associated data
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        Task RemoveKeys(IEnumerable<TKey> keys, bool autoReindex = true);
        /// <summary>
        /// Delete all keys with data except the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        Task RemoveAllExceptKey(TKey key, bool autoReindex = true);
        /// <summary>
        /// Delete all keys with data other than the specified ones
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        Task RemoveAllExceptKeys(IEnumerable<TKey> keys, bool autoReindex = true);
    }
}
