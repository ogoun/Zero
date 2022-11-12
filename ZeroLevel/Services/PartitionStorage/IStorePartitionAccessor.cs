using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Provides read-write operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionAccessor<TKey, TInput, TValue>
        : IDisposable
    {
        /// <summary>
        /// Save one record
        /// </summary>
        void Store(TKey key, TInput value);
        /// <summary>
        /// Complete the recording and perform the conversion of the records from 
        /// (TKey; TInput) to (TKey; TValue)
        /// </summary>
        void CompleteStoreAndRebuild();
        /// <summary>
        /// Find in catalog partition by key
        /// </summary>
        StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key);
        /// <summary>
        /// Find in catalog partition by keys
        /// </summary>
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys);
        /// <summary>
        /// Has any files
        /// </summary>
        int CountDataFiles();
        /// <summary>
        /// Remove all files
        /// </summary>
        void DropData();
    }
}
