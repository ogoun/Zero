namespace ZeroLevel.Services.PartitionStorage
{
    public class SearchResult<TKey, TValue>
    {
        public bool Success { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}
