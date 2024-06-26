﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroLevel.Services.PartitionStorage.Interfaces;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Partition store interface
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Value type</typeparam>
    /// <typeparam name="TValue">The type of compressed array of values for the key</typeparam>
    /// <typeparam name="TMeta">Metadata for creating or searching for a partition</typeparam>
    public interface IStore<TKey, TInput, TValue, TMeta>
    {
        /// <summary>
        /// Returns an object to create a partition
        /// </summary>
        IStorePartitionBuilder<TKey, TInput, TValue> CreateBuilder(TMeta info);
        /// <summary>
        /// Returns an object to overwrite data in an existing partition
        /// </summary>
        IStorePartitionMergeBuilder<TKey, TInput, TValue> CreateMergeAccessor(TMeta info, Func<TValue, IEnumerable<TInput>> decompressor);
        /// <summary>
        /// Creates an object to access the data in the partition
        /// </summary>
        IStorePartitionAccessor<TKey, TInput, TValue> CreateAccessor(TMeta info);
        /// <summary>
        /// Performs a search for data in the repository
        /// </summary>
        IAsyncEnumerable<KVM<TKey, TValue, TMeta>> Search(StoreSearchRequest<TKey, TMeta> searchRequest);
        /// <summary>
        /// bypass all key value by meta
        /// </summary>
        IAsyncEnumerable<KV<TKey, TValue>> Bypass(TMeta meta);
        /// <summary>
        /// bypass all keys by meta
        /// </summary>
        IAsyncEnumerable<TKey> BypassKeys(TMeta meta);
        /// <summary>
        /// true - if key exists
        /// </summary>
        Task<bool> Exists(TMeta meta, TKey key);
        /// <summary>
        /// Deleting a partition
        /// </summary>
        void RemovePartition(TMeta info);
        /// <summary>
        /// Remove all cached data accessors
        /// </summary>
        void DropCache();
    }
}
