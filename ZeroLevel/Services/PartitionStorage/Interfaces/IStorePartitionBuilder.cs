﻿using System.Collections.Generic;

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
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TInput>> Iterate();
        /// <summary>
        /// Writing a key-value pair
        /// </summary>
        void Store(TKey key, TInput value);
        /// <summary>
        /// Called after all key-value pairs are written to the partition
        /// </summary>
        void CompleteAdding();
        /// <summary>
        /// Perform the conversion of the records from (TKey; TInput) to (TKey; TValue). Called after CompleteAdding
        /// </summary>
        void Compress();
        /// <summary>
        /// Rebuilds index files. Only for compressed data. 
        /// </summary>
        void RebuildIndex();
    }
}
