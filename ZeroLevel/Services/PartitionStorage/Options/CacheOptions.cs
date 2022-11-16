namespace ZeroLevel.Services.PartitionStorage
{
    public class CacheOptions
    {
        public bool UsePersistentCache { get; set; }

        public string PersistentCacheFolder { get; set; } = "cachee";

        public int PersistentCacheRemoveTimeoutInSeconds { get; set; } = 3600;

        public bool UseMemoryCache { get; set; }

        public int MemoryCacheLimitInMb { get; set; } = 1024;

        public int MemoryCacheRemoveTimeoutInSeconds { get; set; } = 900;
    }
}
