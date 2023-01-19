namespace ZeroLevel.Services.PartitionStorage
{
    /*TODO IN FUTURE*/
    internal struct ValueIndex<TValue>
    {
        public TValue Value { get; set; }
        public long Offset { get; set; }
    }
}
