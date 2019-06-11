namespace ZeroLevel.SqlServer
{
    public interface IVersionedEntity : IEntity
    {
        long Version { get; }
    }
}
