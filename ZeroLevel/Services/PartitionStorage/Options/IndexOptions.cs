namespace ZeroLevel.Services.PartitionStorage
{
    public class IndexOptions
    {
        public bool Enabled { get; set; }
        public int FileIndexCount { get; set; } = 64;
    }
}
