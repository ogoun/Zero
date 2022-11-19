namespace ZeroLevel.Services.PartitionStorage
{
    public enum SearchResult
    {
        Success,
        NotFound,
        FileLocked
    }

    public class StorePartitionKeyValueSearchResult<TKey, TValue>
    {
        public SearchResult Status { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}
