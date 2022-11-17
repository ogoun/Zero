﻿using System.Collections.Generic;

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
        /// Rebuild indexes
        /// </summary>
        void RebuildIndex();
        /// <summary>
        /// Find in catalog partition by key
        /// </summary>
        StorePartitionKeyValueSearchResult<TKey, TValue> Find(TKey key);
        /// <summary>
        /// Find in catalog partition by keys
        /// </summary>
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Find(IEnumerable<TKey> keys);
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> Iterate();
        IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>> IterateKeyBacket(TKey key);

        void RemoveKey(TKey key);
        void RemoveKeys(IEnumerable<TKey> keys);
        void RemoveAllExceptKey(TKey key);
        void RemoveAllExceptKeys(IEnumerable<TKey> keys);
    }
}
