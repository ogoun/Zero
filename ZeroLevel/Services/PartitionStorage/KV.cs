namespace ZeroLevel.Services.PartitionStorage
{
    public record KV<TKey, TValue>(TKey Key, TValue Value);
    public record KVM<TKey, TValue, TMeta>(TKey Key, TValue Value, TMeta Meta);
}
