namespace ZeroLevel.Services.PartitionStorage
{
    internal interface IStorePartitionIndex<TKey>
    {
        KeyIndex<TKey> GetOffset(TKey key);
        KeyIndex<TKey>[] GetOffset(TKey[] keys, bool inOneGroup);
    }
}
