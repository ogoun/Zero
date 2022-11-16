namespace ZeroLevel.Services.PartitionStorage
{
    public class StorePartitionKeyValueSearchResult<TKey, TValue>
    {
        public bool Found { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}
