using System;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Provides common operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionBase<TKey, TInput, TValue>
        : IDisposable
    {
        string GetCatalogPath();
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
