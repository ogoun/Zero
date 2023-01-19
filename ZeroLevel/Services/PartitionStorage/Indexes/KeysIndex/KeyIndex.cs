namespace ZeroLevel.Services.PartitionStorage
{
    internal struct KeyIndex<TKey>
    {
        public TKey Key { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
    }
}
