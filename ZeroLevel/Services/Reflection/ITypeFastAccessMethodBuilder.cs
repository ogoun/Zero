using System;
using System.Reflection;

namespace ZeroLevel.Services.Reflection
{
    public interface ITypeFastAccessMethodBuilder
    {
        Func<object, object> BuildGetter(PropertyInfo property);

        Func<object, object> BuildGetter(FieldInfo field);

        Action<object, object> BuildSetter(PropertyInfo property);

        Action<object, object> BuildSetter(FieldInfo field);
    }
}