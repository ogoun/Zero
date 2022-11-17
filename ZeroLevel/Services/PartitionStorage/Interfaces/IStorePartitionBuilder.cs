using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    public class InsertValue<TKey, TInput>
    {
        public TKey Key;
        public TInput Value;
    }

    /// <summary>
    /// Provides write operations in catalog partition
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TInput">Type of one input value</typeparam>
    /// <typeparam name="TValue">Type of records aggregate</typeparam>
    public interface IStorePartitionBuilder<TKey, TInput, TValue>
       : IStorePartitionBase<TKey, TInput, TValue>
    {
        /// <summary>
        /// Save one record
        /// </summary>
        void Store(TKey key, TInput value);
        /// <summary>
        /// Complete the recording and perform the conversion of the records from 
        /// (TKey; TInput) to (TKey; TValue)
        /// </summary>
        void CompleteAddingAndCompress();
        void RebuildIndex();
    }
}
