using System;

namespace ZeroLevel.MsSql
{
    public interface IEntity : ICloneable
    {
        Guid Id { get; }
    }

    public interface IEntity<TKey> : ICloneable
    {
        TKey Id { get; }
    }
}
