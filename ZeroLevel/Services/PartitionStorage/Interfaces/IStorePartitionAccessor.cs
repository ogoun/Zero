using System.Collections.Generic;

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
        void RebuildIndex();
        /// <summary>
        /// Search in a partition for a specified key
        /// </summary>
        StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key);
        /// <summary>
        /// Search in a partition for a specified keys
        /// </summary>
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys);
        /// <summary>
        /// Iterating over all recorded data
        /// </summary>
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Iterate();
        /// <summary>
        /// Iterating over all recorded data of the file with the specified key
        /// </summary>
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> IterateKeyBacket(TKey key);
        /// <summary>
        /// Deleting the specified key and associated data
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = false)</param>
        void RemoveKey(TKey key, bool autoReindex = false);
        /// <summary>
        /// Deleting the specified keys and associated data
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        void RemoveKeys(IEnumerable<TKey> keys, bool autoReindex = true);
        /// <summary>
        /// Delete all keys with data except the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        void RemoveAllExceptKey(TKey key, bool autoReindex = true);
        /// <summary>
        /// Delete all keys with data other than the specified ones
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <param name="autoReindex">true - automatically rebuild the index of the file from which data was deleted (default = true)</param>
        void RemoveAllExceptKeys(IEnumerable<TKey> keys, bool autoReindex = true);
    }
}
