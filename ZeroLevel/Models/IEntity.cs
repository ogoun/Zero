using System;

namespace ZeroLevel.Models
{
    public interface IEntity
        : ICloneable
    {
        Guid Id { get; }
    }

    public interface IEntity<TKey>
        : ICloneable
    {
        TKey Id { get; }
    }
}