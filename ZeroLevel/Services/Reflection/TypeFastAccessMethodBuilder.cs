using System;
using System.Reflection;

namespace ZeroLevel.Services.Reflection
{
    public static class TypeFastAccessMethodBuilder
    {
        private readonly static ITypeFastAccessMethodBuilder _builder;

        static TypeFastAccessMethodBuilder()
        {
            _builder = new TypeFastAccessMethodBuilderImpl();
        }

        public static Func<object, object> BuildGetter(this FieldInfo field)
        {
            return _builder.BuildGetter(field);
        }

        public static Func<object, object> BuildGetter(this PropertyInfo property)
        {
            return _builder.BuildGetter(property);
        }

        public static Action<object, object> BuildSetter(this FieldInfo field)
        {
            return _builder.BuildSetter(field);
        }

        public static Action<object, object> BuildSetter(this PropertyInfo property)
        {
            return _builder.BuildSetter(property);
        }
    }
}