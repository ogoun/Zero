namespace ZeroLevel.MsSql
{
    public interface IVersionedEntity : IEntity
    {
        long Version { get; }
    }
}
