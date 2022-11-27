namespace ZeroLevel.Services.PartitionStorage
{
    internal struct ValueIndex<TValue>
    {
        public TValue Value { get; set; }
        public long Offset { get; set; }
    }
}
