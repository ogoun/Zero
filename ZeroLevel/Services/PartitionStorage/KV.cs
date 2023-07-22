namespace ZeroLevel.Services.PartitionStorage
{
    public record KV<TKey, TValue>(TKey Key, TValue Value);
}
