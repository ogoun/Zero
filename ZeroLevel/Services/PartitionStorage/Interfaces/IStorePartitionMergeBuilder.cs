namespace ZeroLevel.Services.PartitionStorage.Interfaces
{
    /// <summary>
    /// Provides write operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionMergeBuilder<TKey, TInput, TValue>
       : IStorePartitionBase<TKey, TInput, TValue>
    {
        long TotalRecords
        {
            get;
        }
        /// <summary>
        /// Writing a key-value pair
        /// </summary>
        void Store(TKey key, TInput value);
        /// <summary>
        /// Perform the conversion of the records from (TKey; TInput) to (TKey; TValue). Called after CompleteAdding
        /// </summary>
        void Compress();
    }
}
