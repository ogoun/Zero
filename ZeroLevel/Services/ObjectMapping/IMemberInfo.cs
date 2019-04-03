using System;

namespace ZeroLevel.Services.ObjectMapping
{
    public interface IMemberInfo
    {
        /// <summary>
        /// true - if field, else - property
        /// </summary>
        bool IsField { get; }

        string Name { get; }
        Type ClrType { get; }
        Action<object, object> Setter { get; }
        Func<object, object> Getter { get; }
    }
}