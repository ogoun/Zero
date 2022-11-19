namespace ZeroLevel.Services.PartitionStorage
{
    internal interface IStorePartitionIndex<TKey>
    {
        /// <summary>
        /// Search for the offset of the closest to the specified key.
        /// </summary>
        KeyIndex<TKey> GetOffset(TKey key);
        /// <summary>
        /// Search for offsets of the keys closest to the specified ones.
        /// </summary>
        KeyIndex<TKey>[] GetOffset(TKey[] keys, bool inOneGroup);
    }
}
