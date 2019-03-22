using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.ObjectMapping
{
    public interface IMapper
    {
        IEnumerable<string> MemberNames { get; }
        IMemberInfo this[string name] { get; }
        IEnumerable<IMemberInfo> Members { get; }
        Type EntityType { get; }
        void TraversalMembers(Action<IMemberInfo> callback);
        void TraversalMembers(Func<IMemberInfo, bool> callback);
        void SetTypeConverter(Func<IMemberInfo, object, object> converter);
        bool Exists(string name);

        void Set(object instance, string name, object value);
        object Get(object instance, string name);
        T Get<T>(object instance, string name);
        T GetOrDefault<T>(object instance, string name, T defaultValue);
        object GetOrDefault(object instance, string name, object defaultValue);
    }
}
